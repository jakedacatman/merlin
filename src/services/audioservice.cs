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
using YoutubeExplode.Videos.Streams;

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

        public async Task<AudioPlayer> ConnectAsync(SocketTextChannel textChannel, SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;
            var currUser = channel.Guild.CurrentUser;

            if (currUser.VoiceChannel != null)
                await channel.DisconnectAsync();
            
            var connection = await channel.ConnectAsync(true, false);

            var np = new AudioPlayer(id, channel, connection, textChannel);

            if (!GetConnection(id, out var curr))
                _connections.Add(np);
            else
            {
                _connections.Remove(curr);
                _connections.Add(np);
            }

            await textChannel.SendMessageAsync($"Joined `{channel.Name}` and will send messages in `{textChannel.Name}`.");

            return np;
        }

        public async Task DisconnectAsync(SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;

            await channel.DisconnectAsync();

            if (GetConnection(id, out var connection))
            {
                _connections.Remove(connection);
                connection.Dispose();
            }
        }

        private readonly SemaphoreSlim enq = new SemaphoreSlim(1, 1);
        public async Task Enqueue(SocketTextChannel textChannel, SocketVoiceChannel vc, Song song)
        {
            await enq.WaitAsync();
            try
            {
                var id = vc.Guild.Id;
                if (!GetConnection(id, out var player))
                    player = await ConnectAsync(textChannel, vc);

                song.Info = await GetAudioInfoAsync(song.Url);
                
                player.Enqueue(song);
                SongAdded?.Invoke(id, player, song);
            }
            finally 
            {
                enq.Release(); 
            }
        }
        public async Task EnqueueMany(SocketTextChannel textChannel, SocketVoiceChannel vc, IEnumerable<Song> songs)
        {
            await enq.WaitAsync();
            try
            {
                var id = vc.Guild.Id;
                if (!GetConnection(id, out var player))
                    player = await ConnectAsync(textChannel, vc);
                
                player.EnqueueMany(songs);
                SongAdded?.Invoke(id, player, songs.First());
            }
            finally 
            {
                enq.Release(); 
            }

            Task.Run(async () =>
            {
                foreach (var song in songs)
                    song.Info = await GetAudioInfoAsync(song.Url);
            });
        }

        public void RemoveAt(ulong id, int index)
        {
            if (!GetConnection(id, out var c))
                return;

            var queue = c.Queue;

            if (index < 0 || index >= queue.Count)
                return;

            queue.RemoveAt(index);
        }

        public void Shuffle(ulong id)
        {
            if (!GetConnection(id, out var c))
                return;

            c.Shuffle();
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

        public async Task OnVoiceUpdate(SocketUser user, SocketVoiceState oldS, SocketVoiceState newS)
        {
            if (user != _client.CurrentUser) return;

            var oldVc = oldS.VoiceChannel;
            var newVc = newS.VoiceChannel;

            if (oldVc == null) return;

            if (newVc == null && GetConnection(oldVc.Guild.Id, out var connection))
            {
                var queue = connection.Queue;
                queue.RemoveRange(0, queue.Count);
                await DisconnectAsync(connection.Channel);
            }
        }

        public async Task PlayAsync(ulong id)
        {
            if (!GetConnection(id, out var player))
                throw new InvalidOperationException("Not connected to a voice channel.");

            await PlayAsync(player);
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

                if (!GetConnection(id, out var connection))
                    throw new InvalidOperationException("Not connected to a voice channel.");

                var channel = connection.Channel;
                if (channel.Guild.CurrentUser.VoiceChannel != channel)
                    await ConnectAsync(player.TextChannel, channel);

                if (connection.IsPlaying)
                    return;

                var discord = connection.Stream;

                var info = song.Info ?? await GetAudioInfoAsync(song.Url);

                song.Size = info.Size.TotalBytes;

                using (var str = await GetAudioAsync(info))
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

                    var skipCts = new CancellationTokenSource();
                    var token = skipCts.Token;
                    
                    player.SongSkipped += (AudioPlayer player, Song song) =>
                    {
                        skipCts.Cancel();
                        return Task.CompletedTask;
                    };
                        
                    var download = Task.Run(async () => 
                    {
                        try
                        {
                            do
                            {
                                bytesDown = await str.ReadAsync(bufferDown, 0, block_size, token);
                                await downloadStream.WriteAsync(bufferDown, 0, bytesDown, token);
                            }
                            while (bytesDown > 0);
                        }
                        finally
                        {
                            await downloadStream.FlushAsync();
                            downloadStream.CompleteWriting();
                        }
                    }, token);

                    var read = Task.Run(async () => 
                    {
                        try
                        {
                            do
                            {
                                bytesRead = await downloadStream.ReadAsync(bufferRead, 0, block_size, token);
                                await input.WriteAsync(bufferRead, 0, bytesRead, token);
                            }
                            while (bytesRead > 0);
                        }
                        finally
                        {
                            await input.FlushAsync();
                        }
                    }, token);


                    var hasHadFullChunkYet = false;
                    var write = Task.Run(async () => 
                    {
                        try
                        {
                            do 
                            {
                                if (hasHadFullChunkYet && bytesConverted < block_size) //if bytesConverted is less than here, then the last (small) chunk is done
                                    break;

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
                            ffmpeg.Kill();
                        }
                    }, token);

                    try
                    {
                        await Task.WhenAll(download, read, write);
                    }
                    catch (OperationCanceledException)
                    {
                        
                    }

                    player.IsPlaying = false;
                    player.IsSkipping = false;
                    player.Current = null;

                    SongEnded?.Invoke(song, connection);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<AudioOnlyStreamInfo> GetAudioInfoAsync(string ytUrl)
        {
            var info = await yt.Videos.Streams.GetManifestAsync(new VideoId(ytUrl));
            
            return info.GetAudioOnly().OrderByDescending(x => x.Bitrate).First();
        }

        private async Task<Stream> GetAudioAsync(AudioOnlyStreamInfo info) => await yt.Videos.Streams.GetAsync(info);

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

        public bool IsConnected(ulong id) => GetConnection(id, out var _);

        public bool HasSongs(ulong id) => GetConnection(id, out var c) && (GetCurrent(id) != null || c.Queue.Any());

        public async Task<int> SkipAsync(SocketGuildUser skipper)
        {
            var id = skipper.Guild.Id;
            if (!GetConnection(id, out var c))
                return 0;

            return await c.SkipAsync(skipper);
        }

        public Song GetCurrent(ulong id)
        {
            if (GetConnection(id, out var connection))
                return connection.Current;

            return null;
        }

        public List<string> GetQueue(ulong id)
        {
            var items = new List<string>();

            if (GetConnection(id, out var connection))
            {
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
            if (GetConnection(id, out var c))
                return c.Queue;

            return new List<Song>();
        }

        private bool GetConnection(ulong id, out AudioPlayer connection)
        {
            connection = null;
            var exists = _connections.Any(x => x.GuildId == id);

            if (exists)
                connection = _connections.First(x => x.GuildId == id);

            return exists;
        }
    }
}