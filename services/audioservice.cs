using System;
using System.IO;
using System.Linq;
using YoutubeExplode;
using System.Threading;
using Nerdbank.Streams;
using Discord.WebSocket;
using donniebot.classes;
using System.Diagnostics;
using YoutubeExplode.Videos;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace donniebot.services
{
    public class AudioService
    {
        private List<AudioPlayer> _connections = new List<AudioPlayer>();
        private readonly DiscordShardedClient _client;
        private readonly NetService _net;
        private YoutubeClient yt = new YoutubeClient();

        public AudioService(DiscordShardedClient client, NetService net)
        {
            _client = client;
            _net = net;

            SongAdded += OnSongAdded;
            SongEnded += OnSongEnded;
        }

        public event Func<ulong, AudioPlayer, Song, Task> SongAdded;
        public event Func<AudioPlayer, Task> SongEnded;

        public async Task ConnectAsync(SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;
            var currUser = channel.Guild.CurrentUser;

            if (currUser.VoiceChannel != null)
                await channel.DisconnectAsync();
            
            var connection = await channel.ConnectAsync(true, false);

            if (!HasConnection(id))
                _connections.Add(new AudioPlayer(id, connection));
            else
            {
                var curr = GetConnection(id);
                _connections.Remove(curr);
                _connections.Add(new AudioPlayer(id, connection));
            }
        }

        public async Task DisconnectAsync(SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;

            await channel.DisconnectAsync();

            if (HasConnection(id))
            {
                var connection = GetConnection(id);
                _connections.Remove(connection);
                connection.Dispose();
            }
        }

        private readonly SemaphoreSlim enq = new SemaphoreSlim(1, 1);
        public async Task Enqueue(SocketVoiceChannel vc, Song song)
        {
            await enq.WaitAsync();
            try
            {
                var id = vc.Guild.Id;
                if (!_connections.Any(x => x.GuildId == id))
                    await ConnectAsync(vc);
                
                var player = _connections.First(x => x.GuildId == id);
                player.Enqueue(song);
                SongAdded?.Invoke(id, player, song);
            }
            finally 
            {
                enq.Release(); 
            }
        }

        public async Task OnSongAdded(ulong id, AudioPlayer player, Song s)
        {
            if (!player.IsPlaying)
                await PlayAsync(player);
        }

        public async Task OnSongEnded(AudioPlayer player)
        {
            if (player.Queue.Count > 0)
                await PlayAsync(player);
        }

        public async Task PlayAsync(AudioPlayer player)
        {
            try
            {
                var id = player.GuildId;
                if (player.Queue.Count == 0)
                    return;

                var song = player.Pop();

                if (!HasConnection(id))
                    throw new InvalidOperationException("Not connected to a voice channel.");

                var connection = GetConnection(id);

                using (var str = await GetAudioAsync(song.Url))
                using (var downloadStream = new SimplexStream())
                using (var ffmpeg = CreateStream())
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var input = ffmpeg.StandardInput.BaseStream)

                using (var discord = connection.Stream)
                {
                    player.IsPlaying = true;

                    int block_size = 4096;

                    var bufferDown = new byte[block_size];
                    var bufferRead = new byte[block_size];
                    var bufferWrite = new byte[block_size];

                    var bytesDown = 0;
                    var bytesRead = 0;
                    var bytesConverted = 0;
                        
                    var download = Task.Run(async () => 
                    {
                        try
                        {
                            do
                            {
                                bytesDown = await str.ReadAsync(bufferDown, 0, block_size);
                                await downloadStream.WriteAsync(bufferDown, 0, bytesDown);
                            }
                            while (bytesDown > 0);
                        }
                        finally
                        {
                            await downloadStream.FlushAsync();
                        }

                        downloadStream.CompleteWriting();
                    });

                    var read = Task.Run(async () => 
                    {
                        try
                        {
                            do
                            {
                                bytesRead = await downloadStream.ReadAsync(bufferRead, 0, block_size);
                                await input.WriteAsync(bufferRead, 0, bytesRead);
                            }
                            while (bytesRead > 0);
                        }
                        finally
                        {
                            await input.FlushAsync();
                        }
                    });

                    var write = Task.Run(async () => 
                    {
                        try
                        {
                            do 
                            {
                                bytesConverted = await output.ReadAsync(bufferWrite, 0, block_size);
                                await discord.WriteAsync(bufferWrite, 0, bytesConverted);
                            }
                            while (bytesConverted > 0);
                        }
                        finally 
                        {
                            await discord.FlushAsync();
                        }
                    });

                    Task.WaitAll(download, read, write);
                }

                connection.UpdateStream();
                player.IsPlaying = false;

                SongEnded?.Invoke(connection);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<Stream> GetAudioAsync(string ytUrl)
        {
            var info = await yt.Videos.Streams.GetManifestAsync(new VideoId(ytUrl));
            
            return await yt.Videos.Streams.GetAsync(info.GetAudioOnly().OrderByDescending(x => x.Bitrate).First());
        }
        public async Task<Song> CreateSongAsync(string queryOrUrl, ulong guildId, ulong userId)
        {
            Video video;

            if (!Uri.IsWellFormedUriString(queryOrUrl, UriKind.Absolute) || !new Uri(queryOrUrl).Host.Contains("youtube"))
                video = await GetVideoAsync(queryOrUrl);
            else
                video = await yt.Videos.GetAsync(new VideoId(queryOrUrl));

            var info = new SongInfo(video.Title, video.Url, video.Thumbnails.MediumResUrl, video.Author);

            return new Song(info, userId, guildId);
        }

        private Process CreateStream()
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            });
        }

        private async Task<Video> GetVideoAsync(string query) => await yt.Search.GetVideosAsync(query).FirstAsync();

        public bool IsConnected(ulong id) => HasConnection(id);

        private AudioPlayer GetConnection(ulong id) => _connections.First(x => x.GuildId == id);

        private bool HasConnection(ulong id) => _connections.Any(x => x.GuildId == id);
    }
}