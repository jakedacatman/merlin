using System;
using System.IO;
using System.Linq;
using YoutubeExplode;
using System.Threading;
using Nerdbank.Streams;
using Discord;
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
        private readonly RandomService _rand;
        private YoutubeClient yt = new YoutubeClient();

        public AudioService(DiscordShardedClient client, NetService net, RandomService rand)
        {
            _client = client;
            _net = net;
            _rand = rand;

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
                await connection.LeaveAsync();

                _connections.Remove(connection);
                connection.Dispose();
            }
        }

        public async Task AddAsync(SocketGuildUser user, SocketTextChannel channel, string queryOrUrl, bool shuffle = false, int? position = null)
        {
            var id = user.Guild.Id;
            var vc = user.VoiceChannel;
            var uId = user.Id;

            if (!IsConnected(id))
            {
                if (vc == null)
                {
                    await channel.SendMessageAsync("You must be in a voice channel.");
                    return;
                }
            }

                if (string.IsNullOrWhiteSpace(queryOrUrl))
                {
                    await PlayAsync(id);
                    
                    return;
                }

                var estTime = GetConnection(id, out var con) ? TimeSpan.FromSeconds(con.Queue
                    .Sum(x => x.Length.TotalSeconds) + (con.Current?.Length.TotalSeconds - con.Position.TotalSeconds ?? 0))
                    .ToString(@"hh\:mm\:ss")
                    : "Now";

                if (estTime == "00:00:00") estTime = "Now";

                if (Uri.IsWellFormedUriString(queryOrUrl, UriKind.Absolute) && (queryOrUrl.Contains("&list=") || queryOrUrl.Contains("?list=")))
                {
                    var playlist = await GetPlaylistAsync(queryOrUrl, id, user.Id);

                    await EnqueueMany(channel, vc, playlist.Songs, shuffle, position);

                    await channel.SendMessageAsync(embed: new EmbedBuilder()
                        .WithTitle("Added playlist")
                        .WithFields(new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder().WithName("Title").WithValue(playlist.Title).WithIsInline(false),
                            new EmbedFieldBuilder().WithName("Author").WithValue(playlist.Author).WithIsInline(true),
                            new EmbedFieldBuilder().WithName("Count").WithValue(playlist.Songs.Count).WithIsInline(true),
                            new EmbedFieldBuilder().WithName("URL").WithValue(playlist.Url).WithIsInline(false),
                            new EmbedFieldBuilder().WithName("Estimated time").WithValue(estTime).WithIsInline(true)
                        })
                        .WithColor(_rand.RandomColor())
                        .WithThumbnailUrl(playlist.ThumbnailUrl)
                        .WithCurrentTimestamp()
                        .Build());

                    return;
                }

                var song = await CreateSongAsync(queryOrUrl, id, uId);

                await channel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle("Added song")
                    .WithFields(new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder().WithName("Title").WithValue(song.Title).WithIsInline(false),
                        new EmbedFieldBuilder().WithName("Author").WithValue(song.Author).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("URL").WithValue(song.Url).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Estimated time").WithValue(estTime).WithIsInline(true)
                    })
                    .WithColor(_rand.RandomColor())
                    .WithThumbnailUrl(song.ThumbnailUrl)
                    .WithCurrentTimestamp()
                .Build());

                await Enqueue(channel, vc, song, shuffle, position);
        }

        public async Task ResumeAsync(ulong guildId)
        {
            if (!GetConnection(guildId, out var con)) return;
            await con.ResumeAsync();
        }

        public async Task PauseAsync(ulong guildId)
        {
            if (!GetConnection(guildId, out var con)) return;
            await con.PauseAsync();
        }

        private readonly SemaphoreSlim enq = new SemaphoreSlim(1, 1);
        public async Task Enqueue(SocketTextChannel textChannel, SocketVoiceChannel vc, Song song, bool shuffle = false, int? position = null)
        {
            await enq.WaitAsync();
            try
            {
                var id = vc.Guild.Id;
                if (!GetConnection(id, out var player))
                    player = await ConnectAsync(textChannel, vc);

                song.Info = await GetAudioInfoAsync(song.Url);
                
                player.Enqueue(song, position);
                if (shuffle) Shuffle(player.GuildId);
                SongAdded?.Invoke(id, player, song);
            }
            finally 
            {
                enq.Release(); 
            }
        }
        public async Task EnqueueMany(SocketTextChannel textChannel, SocketVoiceChannel vc, IEnumerable<Song> songs, bool shuffle = false, int? position = null)
        {
            await enq.WaitAsync();
            try
            {
                var id = vc.Guild.Id;
                if (!GetConnection(id, out var player))
                    player = await ConnectAsync(textChannel, vc);
                
                player.EnqueueMany(songs, position);
                if (shuffle) Shuffle(player.GuildId);
                SongAdded?.Invoke(id, player, songs.First());
            }
            finally 
            {
                enq.Release(); 
            }

            #pragma warning disable CS4014
            Task.Run(async () => //let it run in the background... we don't really care about the output
            {
                foreach (var song in songs)
                    song.Info = await GetAudioInfoAsync(song.Url);
            });
            #pragma warning restore CS4014
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
                await PlayAsync(id);
        }

        public async Task OnSongEnded(Song s, AudioPlayer player)
        {
            if (player.Queue.Count > 0)
            {
                if (player.GetListeningUsers().Any())
                    await PlayAsync(player.GuildId);
                else
                    player.Dispose();
            }
        }

        public async Task OnVoiceUpdate(SocketUser user, SocketVoiceState oldS, SocketVoiceState newS)
        {
            if (user != _client.CurrentUser) return;

            var oldVc = oldS.VoiceChannel;
            var newVc = newS.VoiceChannel;

            if (oldVc == null) return;

            if (GetConnection(oldVc.Guild.Id, out var connection))
                if (newVc == null)
                {
                    var queue = connection.Queue;
                    queue.RemoveRange(0, queue.Count);
                    await DisconnectAsync(connection.VoiceChannel);
                }
                else
                    await connection.UpdateAsync(newVc);
        }

        public async Task PlayAsync(ulong id)
        {
            if (!GetConnection(id, out var connection) || connection.VoiceChannel == null)
                throw new InvalidOperationException("Not connected to a voice channel.");

            if (!connection.Queue.Any())
                    return;

            var song = connection.Pop();
            connection.Current = song;

            try
            {

                var channel = connection.VoiceChannel;
                if (channel.Guild.CurrentUser.VoiceChannel != channel)
                    await ConnectAsync(connection.TextChannel, channel);

                if (connection.IsPlaying)
                    return;

                if (connection.HasDisconnected)
                    await connection.UpdateAsync(channel);

                var discord = connection.Stream;

                var info = song.Info ?? await GetAudioInfoAsync(song.Url);

                song.Size = info.Size.TotalBytes;

                using (var str = await GetAudioAsync(info))
                using (var downloadStream = new SimplexStream())
                using (var ffmpeg = CreateStream())
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var input = ffmpeg.StandardInput.BaseStream)
                {
                    connection.IsPlaying = true;

                    const int block_size = 4096; //4 KiB

                    var bufferDown = new byte[block_size];
                    var bufferRead = new byte[block_size];
                    var bufferWrite = new byte[block_size];

                    int bytesDown = 0, bytesRead = 0, bytesConverted = 0;

                    var skipCts = new CancellationTokenSource();
                    var token = skipCts.Token;

                    var pauseCts = new CancellationTokenSource();
                    
                    connection.SongSkipped += (AudioPlayer _, Song song) =>
                    {
                        skipCts.Cancel();
                        return Task.CompletedTask;
                    };

                    connection.SongPaused += (AudioPlayer _, bool state) =>
                    {
                        if (state)
                            pauseCts.Cancel();
                        else
                            pauseCts = new CancellationTokenSource();
                        
                        return Task.CompletedTask;
                    };
                        
                    var download = Task.Run(async () => 
                    {
                        try
                        {
                            do
                            {
                                bytesDown = await str.ReadAsync(bufferDown, 0, block_size, token);
                                connection.BytesDownloaded += (uint)bytesDown;

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
                                while (connection.IsPaused) //don't write to discord while paused
                                    try
                                    {
                                        await Task.Delay(-1, pauseCts.Token); //wait forever (until token is called)
                                    }
                                    catch (TaskCanceledException)
                                    {

                                    }

                                if (hasHadFullChunkYet && bytesConverted < block_size) //if bytesConverted is less than here, then the last (small) chunk is done
                                    break;

                                bytesConverted = await output.ReadAsync(bufferWrite, 0, block_size, token);
                                connection.BytesPlayed += (uint)bytesConverted;

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

                    try //allows for skipping
                    {
                        Task.WaitAll(new[] { download, read, write }, token);
                    }
                    catch (OperationCanceledException) //allows for skipping
                    {
                        
                    }
                    catch (AggregateException) //allows for skipping
                    {
                        
                    }
                }
            }
            catch (Discord.Net.WebSocketClosedException)
            {
                
            }
            finally
            {
                connection.BytesDownloaded = 0;
                connection.BytesPlayed = 0;
                connection.IsPlaying = false;
                connection.Current = null;
                SongEnded?.Invoke(song, connection);
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

        public async Task<int> SkipAsync(SocketGuildUser skipper) => GetConnection(skipper.Guild.Id, out var c) ? await c.SkipAsync(skipper) : 0;

        public string GetSongPosition(ulong id) => GetConnection(id, out var con) ? $"{con.Position.ToString(@"hh\:mm\:ss")}/{con.Current.Length.ToString(@"hh\:mm\:ss")}" : null;
        public double GetDownloadedPercent(ulong id) => GetConnection(id, out var con) ? (double)con.BytesDownloaded / con.Current.Size : 0d;

        public List<SocketGuildUser> GetListeningUsers(ulong id) => GetConnection(id, out var connection) ? connection.GetListeningUsers().ToList() : new List<SocketGuildUser>();

        public Song GetCurrent(ulong id) => GetConnection(id, out var connection) ? connection.Current : null;

        public List<string> GetQueue(ulong id)
        {
            var items = new List<string>();

            if (GetConnection(id, out var connection))
            {
                var queue = connection.Queue;
                var gId = connection.GuildId;

                if (connection.Current != null) 
                {
                    items.Add($"__**{1}**: {connection.Current.Title} (queued by {GetMention(gId, connection.Current.QueuerId)})__");

                    for (int i = 0; i < queue.Count; i++)
                        items.Add($"**{i+2}**: {queue[i].Title} (queued by {GetMention(gId, queue[i].QueuerId)})");
                }
                else
                    for (int i = 0; i < queue.Count; i++)
                        items.Add($"**{i+1}**: {queue[i].Title} (queued by {GetMention(gId, queue[i].QueuerId)})");
            }

            return items;
        }
        public List<Song> GetRawQueue(ulong id) => GetConnection(id, out var c) ? c.Queue : new List<Song>();

        private string GetMention(ulong guildId, ulong userId) => 
            _client
                .GetGuild(guildId)
                .GetUser(userId)
                .Mention;

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