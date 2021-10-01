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
        private readonly DbService _db;
        private readonly RandomService _rand;
        private YoutubeClient yt = new YoutubeClient();

        public AudioService(DiscordShardedClient client, NetService net, DbService db, RandomService rand)
        {
            _client = client;
            _net = net;
            _db = db;
            _rand = rand;

            SongAdded += OnSongAddedAsync;
            SongEnded += OnSongEndedAsync;

            _client.UserVoiceStateUpdated += OnVoiceUpdateAsync;
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

        public async Task DisconnectAsync(SocketVoiceChannel channel, ISocketMessageChannel tc = null)
        {
            var id = channel.Guild.Id;

            await channel.DisconnectAsync();

            if (GetConnection(id, out var connection))
            {
                connection.IsPlaying = false;
                await connection.LeaveAsync(true, tc);

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

            if (con != null && position.HasValue && con.Queue.Any())
            {
                var len = con.Queue.Take(position.Value).Sum(x => x.Length.TotalSeconds);

                estTime = TimeSpan.FromSeconds(len +
                    (con.Current?.Length.TotalSeconds - con.Position.TotalSeconds ?? 0))
                    .ToString(@"hh\:mm\:ss");
            }

            if (Uri.IsWellFormedUriString(queryOrUrl, UriKind.Absolute))
            {
                var msg = await channel.SendMessageAsync("Adding your playlist...");

                if (queryOrUrl.Contains("&list=") || queryOrUrl.Contains("?list="))
                {
                    var playlist = await GetPlaylistAsync(queryOrUrl, id, user.Id);

                    await EnqueueManyAsync(channel, vc, playlist.Songs, shuffle, position);

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
                else if (queryOrUrl.Contains("spotify.com"))
                {
                    var spotifyPl = await _net.GetSpotifySongsAsync(queryOrUrl);

                    var playlist = new List<Song>();

                    foreach (var s in spotifyPl)
                    {
                        playlist.Add(await CreateSongAsync(
                            $"{string.Join(", ", s.Track.Artists?.Select(x => x.Name))} {s.Track.Name} audio", //Travis Scott, Drake SICKO MODE audio
                            id, 
                            uId
                        ));
                    }

                    await EnqueueManyAsync(channel, vc, playlist, shuffle, position);

                    await channel.SendMessageAsync(embed: new EmbedBuilder()
                        .WithTitle("Added Spotify playlist")
                        .WithFields(new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder().WithName("Count").WithValue(spotifyPl.Count).WithIsInline(true),
                            new EmbedFieldBuilder().WithName("URL").WithValue(queryOrUrl).WithIsInline(false),
                            new EmbedFieldBuilder().WithName("Estimated time").WithValue(estTime).WithIsInline(true)
                        })
                        .WithColor(_rand.RandomColor())
                        .WithFooter("The song may sound different as it is coming from YouTube, not Spotify.")
                        .WithCurrentTimestamp()
                        .Build());
                }
            }

            var song = await CreateSongAsync(queryOrUrl, id, uId);

            if (song is null)
            {
                await channel.SendMessageAsync("Unable to locate a song with that ID or search query; please try again.");
                return;
            }

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

            await EnqueueAsync(channel, vc, song, shuffle, position);
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
        public async Task EnqueueAsync(SocketTextChannel textChannel, SocketVoiceChannel vc, Song song, bool shuffle = false, int? position = null)
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
        public async Task EnqueueManyAsync(SocketTextChannel textChannel, SocketVoiceChannel vc, IEnumerable<Song> songs, bool shuffle = false, int? position = null)
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
            
            foreach (var song in songs)
                song.Info = await GetAudioInfoAsync(song.Url);
        }

        public bool RemoveAt(ulong id, int index)
        {
            if (!GetConnection(id, out var c))
                return false;

            var queue = c.Queue;

            if (index < 1 || index >= queue.Count)
                return false;

            queue.RemoveAt(index);
            return true;
        }

        public void Shuffle(ulong id)
        {
            if (!GetConnection(id, out var c))
                return;

            c.Shuffle();
        }

        public async Task OnSongAddedAsync(ulong id, AudioPlayer player, Song s)
        {
            if (!player.IsPlaying)
                await PlayAsync(id);
        }

        public async Task OnSongEndedAsync(Song s, AudioPlayer player)
        {
            if (player.Queue.Count > 0)
            {
                if (player.GetListeningUsers().Any())
                    await PlayAsync(player.GuildId);
                else
                    player.Dispose();
            }
        }

        public async Task OnVoiceUpdateAsync(SocketUser user, SocketVoiceState oldS, SocketVoiceState newS)
        {
            try
            {
                if (user != _client.CurrentUser) return;

                var oldVc = oldS.VoiceChannel;
                var newVc = newS.VoiceChannel;

                if (oldVc == null) return;

                if (GetConnection(oldVc.Guild.Id, out var connection))
                    if (newVc == null)
                        await DisconnectAsync(connection.VoiceChannel);
                    else
                    {
                        await connection.PauseAsync();
                        await connection.UpdateAsync(newVc);
                    }
            }
            catch (Discord.Net.WebSocketClosedException e)
            {
                Console.WriteLine($"{e}\n{e.StackTrace}");
            }
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

                song.Size = info.Size.Bytes;

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

                    var pauseCts = new CancellationTokenSource();

                    connection.SongPaused += (AudioPlayer _, bool state) =>
                    {
                        if (state)
                            pauseCts.Cancel();
                        else
                            pauseCts = new CancellationTokenSource();

                        return Task.CompletedTask;
                    };

                    async Task Download(CancellationToken token)
                    {
                        try
                        {
                            do
                            {
                                if (token.IsCancellationRequested) break;

                                bytesDown = await str.ReadAsync(bufferDown, 0, block_size);
                                connection.BytesDownloaded += (uint)bytesDown;

                                await downloadStream.WriteAsync(bufferDown, 0, bytesDown);
                            }
                            while (bytesDown > 0);
                        }
                        finally
                        {
                            downloadStream.CompleteWriting();
                        }
                    }

                    async Task Read(CancellationToken token)
                    {
                        try
                        {
                            do
                            {
                                if (token.IsCancellationRequested) break;

                                bytesRead = await downloadStream.ReadAsync(bufferRead, 0, block_size);
                                await input.WriteAsync(bufferRead, 0, bytesRead);
                            }
                            while (bytesRead > 0);
                        }
                        catch (IOException)
                        {

                        }
                    }

                    var hasHadFullChunkYet = false;
                    async Task Write(CancellationToken token)
                    {
                        try
                        {
                            do
                            {
                                while (connection.IsPaused) //don't write to discord while paused
                                {
                                    try
                                    {
                                        if (token.IsCancellationRequested) return;

                                        await Task.Delay(-1, pauseCts.Token); //wait forever (until token is called)
                                    }
                                    catch (TaskCanceledException)
                                    {

                                    }
                                }
                                
                                if (token.IsCancellationRequested) break;

                                if (hasHadFullChunkYet && bytesConverted < block_size) //if bytesConverted is less than here, then the last (small) chunk is done
                                    break;

                                bytesConverted = await output.ReadAsync(bufferWrite, 0, block_size);
                                connection.BytesPlayed += (uint)bytesConverted;

                                if (bytesConverted == block_size)
                                    hasHadFullChunkYet = true;

                                await discord.WriteAsync(bufferWrite, 0, bytesConverted);
                            }
                            while (bytesConverted > 0);
                        }
                        finally
                        {
                            ffmpeg.Kill();
                        }
                    }

                    try //allows for skipping
                    {
                        var cts = new CancellationTokenSource();

                        connection.SongSkipped += (AudioPlayer _, Song _) =>
                        {
                            cts.Cancel();
                            return Task.CompletedTask;
                        };

                        await Task.WhenAll(Download(cts.Token), Read(cts.Token), Write(cts.Token));
                    }
                    catch (Exception)
                    {

                    }
                    finally
                    {
                        await discord.FlushAsync();

                        if (!ffmpeg.HasExited) 
                            ffmpeg.Kill();
                    }
                }
            }
            catch (Discord.Net.WebSocketClosedException)
            {
                _connections.Remove(connection);
                connection.Dispose();
                return;
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
            var id = VideoId.TryParse(ytUrl);
            if (id is null) throw new VideoException("Failed to parse the URL.");

            var info = await yt.Videos.Streams.GetManifestAsync(VideoId.Parse(ytUrl));
            return info
                .GetAudioOnlyStreams()
                .OrderByDescending(x => x.Bitrate)
                .First();
        }

        private async Task<Stream> GetAudioAsync(AudioOnlyStreamInfo info) => await yt.Videos.Streams.GetAsync(info);

        public async Task<Song> CreateSongAsync(string queryOrUrl, ulong guildId, ulong userId)
        {
            SongInfo info;

            if (!Uri.IsWellFormedUriString(queryOrUrl, UriKind.Absolute) || !new Uri(queryOrUrl).Host.Contains("youtube") || !new Uri(queryOrUrl).Host.Contains("youtu.be"))
            {
                var video = await GetVideoAsync(queryOrUrl);
                if (video is null) return null;

                info = new SongInfo(
                    video.Title, 
                    video.Url, 
                    video.Thumbnails
                        .OrderByDescending(x => x.Resolution.Area)
                        .First()
                        .Url, 
                    video.Author.Title, 
                    video.Duration ?? new TimeSpan(0)
                );
            }
            else
            {
                var id = VideoId.TryParse(queryOrUrl);
                if (id is null) throw new VideoException("Invalid video.");

                var video = await yt.Videos.GetAsync(id.Value);
                if (video is null) throw new VideoException("Invalid video.");

                info = new SongInfo(
                    video.Title, 
                    video.Url, 
                    video.Thumbnails
                        .OrderByDescending(x => x.Resolution.Area)
                        .First()
                        .Url,
                    video.Author.Title,
                    video.Duration ?? new TimeSpan(0)
                );
            }

            return new Song(info, userId, guildId);
        }

        public async Task<classes.Playlist> GetPlaylistAsync(string playlistId, ulong guildId, ulong userId)
        {
            var songs = new List<Song>();

            var id = PlaylistId.TryParse(playlistId);
            if (id is null) throw new VideoException("Invalid playlist.");

            var data = await yt.Playlists.GetAsync(id.Value);
            var videos = yt.Playlists.GetVideosAsync(id.Value);

            await foreach (var video in videos)
                songs.Add(new Song(
                    new SongInfo(video.Title,
                        video.Url,
                        video.Thumbnails
                            .OrderByDescending(x => x.Resolution.Area)
                            .First()
                            .Url,
                        video.Author.Title,
                        video.Duration ?? new TimeSpan(0)),
                    userId,
                    guildId));

            return new classes.Playlist(
                songs, 
                data.Title, 
                data.Author.Title, 
                data.Url, 
                data.Thumbnails
                    .OrderByDescending(x => x.Resolution.Area)
                    .First()
                    .Url,
                userId, 
                guildId
            );
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

        private async Task<YoutubeExplode.Search.VideoSearchResult> GetVideoAsync(string query) => await yt.Search.GetVideosAsync(query).FirstOrDefaultAsync();

        public bool IsConnected(ulong id) => GetConnection(id, out var _);

        public bool HasSongs(ulong id) => GetConnection(id, out var c) && (GetCurrent(id) != null || c.Queue.Any());

        public async Task<int> SkipAsync(SocketGuildUser skipper)
        {
            
            if (GetConnection(skipper.Guild.Id, out var c))
            {
                var djRole = _db.GetItem<DjRole>("djroles", LiteDB.Query.EQ("GuildId", skipper.Guild.Id));
                if (skipper.Roles.Any(x => x.Id == djRole?.RoleId))
                    return await c.DoSkipAsync();

                return await c.SkipAsync(skipper);
            }
            else return 0;
        }

        public string GetSongPosition(ulong id) => GetConnection(id, out var con) ? $"{con.Position.ToString(@"hh\:mm\:ss")}/{con.Current.Length.ToString(@"hh\:mm\:ss")}" : null;
        public TimeSpan GetRawPosition(ulong id) => GetConnection(id, out var con) ? con.Current.Length - con.Position : TimeSpan.FromSeconds(0);
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
                    items.Add($"{(connection.IsPlaying ? "▶️" : "⏸️")} __**1**: {connection.Current.Title} (queued by {GetMention(gId, connection.Current.QueuerId)})__");

                    for (int i = 0; i < queue.Count; i++)
                        items.Add($"**{i + 2}**: {queue[i].Title} (queued by {GetMention(gId, queue[i].QueuerId)})");
                }
                else
                    for (int i = 0; i < queue.Count; i++)
                        items.Add($"**{i + 1}**: {queue[i].Title} (queued by {GetMention(gId, queue[i].QueuerId)})");
            }

            return items;
        }
        public List<Song> GetRawQueue(ulong id) => GetConnection(id, out var c) ? c.Queue : new List<Song>();

        private string GetMention(ulong guildId, ulong userId) =>
            _client
                .GetGuild(guildId)
                .GetUser(userId)
                .Mention;

        public bool GetConnection(ulong id, out AudioPlayer connection)
        {
            connection = null;
            var exists = _connections.Any(x => x.GuildId == id);

            if (exists)
                connection = _connections.First(x => x.GuildId == id);

            return exists;
        }
    }
}