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
using YoutubeExplode.Playlists;
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

            _client.UserVoiceStateUpdated += OnVoiceUpdate;
        }

        public event Func<ulong, AudioPlayer, Song, Task> SongAdded;
        public event Func<Song, AudioPlayer, Task> SongEnded;

        public async Task ConnectAsync(SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;
            var currUser = channel.Guild.CurrentUser;

            if (currUser.VoiceChannel != null)
                await channel.DisconnectAsync();
            
            var connection = await channel.ConnectAsync(true, false);

            if (!HasConnection(id))
                _connections.Add(new AudioPlayer(id, channel, connection));
            else
            {
                var curr = GetConnection(id);
                _connections.Remove(curr);
                _connections.Add(new AudioPlayer(id, channel, connection));
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
                if (!HasConnection(id))
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
        public async Task EnqueueMany(SocketVoiceChannel vc, IEnumerable<Song> songs)
        {
            await enq.WaitAsync();
            try
            {
                var id = vc.Guild.Id;
                if (!HasConnection(id))
                    await ConnectAsync(vc);
                
                var player = _connections.First(x => x.GuildId == id);
                player.EnqueueMany(songs);
                SongAdded?.Invoke(id, player, songs.First());
            }
            finally 
            {
                enq.Release(); 
            }
        }

        public void RemoveAt(ulong id, int index)
        {
            if (!HasConnection(id))
                return;

            var queue = GetConnection(id).Queue;

            if (index < 0 || index >= queue.Count)
                return;

            queue.RemoveAt(index);
        }

        public void Shuffle(ulong id)
        {

            if (!HasConnection(id))
                return;

            var queue = GetConnection(id).Queue;
            if (!queue.Any()) return;

            queue.Shuffle();
        }

        public async Task OnSongAdded(ulong id, AudioPlayer player, Song s)
        {
            if (!player.IsPlaying)
                await PlayAsync(player);
        }

        public async Task OnSongEnded(Song s, AudioPlayer player)
        {
            if (player.Queue.Count > 0)
                await PlayAsync(player);
        }

        public Task OnVoiceUpdate(SocketUser user, SocketVoiceState oldS, SocketVoiceState newS)
        {
            if (newS.VoiceChannel == null)
            {
                var queue = GetConnection(newS.VoiceChannel.Id).Queue;
                queue.RemoveRange(0, queue.Count);
            }

            return Task.CompletedTask;
        }

        public async Task PlayAsync(ulong id)
        {
            if (!HasConnection(id))
                throw new InvalidOperationException("Not connected to a voice channel.");

            await PlayAsync(GetConnection(id));
        }
        public async Task PlayAsync(AudioPlayer player)
        {
            try
            {
                var id = player.GuildId;
                if (!player.Queue.Any())
                    return;

                var song = player.Pop();
                player.Current = song;

                if (!HasConnection(id))
                    throw new InvalidOperationException("Not connected to a voice channel.");

                var connection = GetConnection(id);
                var channel = connection.Channel;
                if (channel.Guild.CurrentUser.VoiceChannel != channel)
                    await ConnectAsync(channel);

                if (connection.IsPlaying)
                    return;

                var discord = connection.Stream;

                using (var str = await GetAudioAsync(song.Url))
                using (var downloadStream = new SimplexStream())
                using (var ffmpeg = CreateStream())
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var input = ffmpeg.StandardInput.BaseStream)
                {
                    player.IsPlaying = true;

                    const int block_size = 4096; //4 KiB

                    var bufferDown = new byte[block_size];
                    var bufferRead = new byte[block_size];
                    var bufferWrite = new byte[block_size];

                    var bytesDown = 0;
                    var bytesRead = 0;
                    var bytesConverted = 0;

                    var isWriting = false;

                    var skipCts = new CancellationTokenSource();
                    var token = skipCts.Token;
                        
                    var download = Task.Run(async () => 
                    {
                        try
                        {
                            do
                            {
                                if (player.IsSkipping)
                                {
                                    downloadStream.CompleteWriting();
                                    skipCts.Cancel();
                                    break;
                                }
                                
                                bytesDown = await str.ReadAsync(bufferDown, 0, block_size, token);
                                await downloadStream.WriteAsync(bufferDown, 0, bytesDown, token);
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
                                if (player.IsSkipping)
                                {
                                    skipCts.Cancel();
                                    break;
                                }
                                
                                bytesRead = await downloadStream.ReadAsync(bufferRead, 0, block_size, token);
                                await input.WriteAsync(bufferRead, 0, bytesRead, token);
                            }
                            while (bytesRead > 0);
                        }
                        finally
                        {
                            await input.FlushAsync();
                        }
                    });


                    var hasHadFullChunkYet = false;
                    var write = Task.Run(async () => 
                    {
                        try
                        {
                            do 
                            {
                                if (player.IsSkipping)
                                {
                                    skipCts.Cancel();
                                    break;
                                }

                                if (isWriting && hasHadFullChunkYet && bytesConverted < block_size) //if bytesConverted is less than here, then the last (small) chunk is done
                                    break;

                                isWriting = true;

                                bytesConverted = await output.ReadAsync(bufferWrite, 0, block_size, token);

                                if (bytesConverted == block_size)
                                    hasHadFullChunkYet = true;

                                await discord.WriteAsync(bufferWrite, 0, bytesConverted, token);
                            }
                            while (bytesConverted > 0);
                        }
                        finally 
                        {
                            await discord.FlushAsync();
                        }
                    });

                    Task.WaitAll(download, read, write);

                    player.IsPlaying = false;
                    player.IsSkipping = false;
                    player.Current = null;

                    var finished = player.Pop();
                    SongEnded?.Invoke(finished, connection);
                }
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

            var info = new SongInfo(video.Title, video.Url, video.Thumbnails.MediumResUrl, video.Author, video.Duration);

            return new Song(info, userId, guildId);
        }
        public async Task<classes.Playlist> GetPlaylistAsync(string playlistId, ulong guildId, ulong userId)
        {
            var songs = new List<Song>();
            var id = new PlaylistId(playlistId);

            var data = await yt.Playlists.GetAsync(id);
            var videos = await yt.Playlists.GetVideosAsync(id);

            foreach (var video in videos)
                songs.Add(new Song(
                    new SongInfo(video.Title, 
                        video.Url, 
                        video.Thumbnails.MediumResUrl, 
                        video.Author, 
                        video.Duration),
                    userId, 
                    guildId));

            return new classes.Playlist(songs, data.Title, data.Author, data.Url, data.Thumbnails.MediumResUrl, userId, guildId);
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

        public bool HasSongs(ulong id) => (HasConnection(id) && GetConnection(id).Queue.Any());

        private AudioPlayer GetConnection(ulong id) => _connections.First(x => x.GuildId == id);

        public int Skip(SocketGuildUser skipper)
        {
            var id = skipper.Guild.Id;
            if (!HasConnection(id))
                return 0;

            return GetConnection(id).Skip(skipper);
        }

        public List<string> GetQueue(ulong id)
        {
            var items = new List<string>();

            if (HasConnection(id))
            {
                var connection = GetConnection(id);
                var queue = connection.Queue;

                if (connection.Current != null) 
                {
                    items.Add($"__**{1}**: {connection.Current.Title} (queued by {connection.Current.QueuerId})__");

                    for (int i = 0; i < queue.Count; i++)
                        items.Add($"**{i+2}**: {queue[i].Title} (queued by {queue[i].QueuerId})");
                }
                else
                    for (int i = 0; i < queue.Count; i++)
                        items.Add($"**{i+1}**: {queue[i].Title} (queued by {queue[i].QueuerId})");
            }

            return items;
        }
        public List<Song> GetRawQueue(ulong id)
        {
            var songs = new List<Song>();

            if (HasConnection(id))
                return GetConnection(id).Queue;

            return songs;
        }

        private bool HasConnection(ulong id) => _connections.Any(x => x.GuildId == id);
    }
}