using System;
using System.IO;
using System.Linq;
using YoutubeExplode;
using System.Threading;
using Nerdbank.Streams;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using donniebot.services;
using System.Diagnostics;
using YoutubeExplode.Videos;
using System.Threading.Tasks;
using YoutubeExplode.Playlists;
using System.Collections.Generic;
using YoutubeExplode.Videos.Streams;
using System.Net.Http;

namespace donniebot.classes
{
    public class AudioPlayer : IDisposable
    {
        public ulong GuildId { get; }
        public IAudioClient Connection { get; private set; }
        public SocketVoiceChannel VoiceChannel { get; private set; }
        public SocketTextChannel TextChannel { get; private set; }
        public AudioOutStream Stream { get; private set; }
        public Song Current { get; set; } = null;
        public List<Song> Queue { get; set; }
        public TimeSpan Position 
        { 
            get 
            {
                if (Current is null) return TimeSpan.FromSeconds(0);
                return TimeSpan.FromSeconds(BytesPlayed / 196608d); //ffmpeg output in bytes/second
            }
        }
        public bool HasDisconnected { get; private set; } = false;
        public bool IsPlaying { get; set; } = false;
        public bool IsPaused { get; private set; } = false;
        public bool IsLooping { get; private set; } = false;

        private int _skips = 0;
        private List<ulong> _skippedUsers = new List<ulong>();
        private YoutubeClient yt = new YoutubeClient();
        private HttpClient _hc = new HttpClient();
        private CancellationTokenSource songEndedToken = new CancellationTokenSource();

        private readonly NetService _net;
        private readonly DiscordShardedClient _client;

        internal uint BytesDownloaded { get; set; } = 0;
        internal uint BytesPlayed { get; set; } = 0;

        private const int block_size = 16384; //16 KiB

        public event Func<Song, Task> SongAdded;
        public event Func<Song, Task> SongEnded;
        public event Func<Song, Task> SongSkipped;
        public event Func<bool, Task> SongToggledPlaying;

        public AudioPlayer(ulong id, SocketVoiceChannel channel, IAudioClient client, SocketTextChannel textchannel, DiscordShardedClient shardedClient, NetService net)
        {
            GuildId = id;
            VoiceChannel = channel;
            Connection = client;
            TextChannel = textchannel;
            Stream = client.CreatePCMStream(AudioApplication.Mixed);
            Queue = new List<Song>();
            _net = net;

            _client = shardedClient;

            _client.UserVoiceStateUpdated += OnVoiceUpdateAsync;

            SongAdded += OnSongAddedAsync;
            SongEnded += OnSongEndedAsync;
        }

        public async Task OnSongAddedAsync(Song s)
        {
            if (!IsPlaying)
                await PlayAsync();
        }

        public async Task OnSongEndedAsync(Song s)
        {
            if (IsLooping)
                await EnqueueAsync(TextChannel, VoiceChannel, s, position: 0);

            if (Queue.Any())
            {
                if (GetListeningUsers().Any())
                    await PlayAsync();
                else
                    Dispose();
            }
            else
            {
                Task OnSongAdded(Song _)
                {
                    songEndedToken.Cancel();
                    return Task.CompletedTask;
                }

                SongAdded += OnSongAdded;
                await Task.Delay(300000, songEndedToken.Token); //5 minutes
                if (songEndedToken.IsCancellationRequested)
                    await DisconnectAsync();

                songEndedToken = new CancellationTokenSource();
                SongAdded -= OnSongAdded;
            }
        }

        public async Task OnVoiceUpdateAsync(SocketUser user, SocketVoiceState oldS, SocketVoiceState newS)
        {
            try
            {
                if (user != VoiceChannel.Guild.CurrentUser) return;

                var oldVc = oldS.VoiceChannel;
                var newVc = newS.VoiceChannel;

                if (oldVc is null) return;

               if (newVc is null)
                    await DisconnectAsync();
                else
                {
                    Enqueue(Current, 0);
                    await DoSkipAsync();
                    await UpdateAsync(newVc);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}\n{e.StackTrace}");
            }
        }

        public async Task DisconnectAsync(ISocketMessageChannel tc = null)
        {
            if (tc is null) tc = TextChannel;

            var id = (tc as SocketGuildChannel).Guild.Id;

            await VoiceChannel.DisconnectAsync();

            IsPlaying = false;
            await LeaveAsync(true, tc);
            Dispose();
        }

        public void ToggleLoop() => IsLooping = !IsLooping;

        public void Enqueue(Song s, int? position)
        { 
            if (!position.HasValue)
                Queue.Add(s);
            else
                Queue.Insert(position.Value, s);
        }

        public async Task LeaveAsync(bool sendMessage = true, ISocketMessageChannel tc = null)
        {
            _skips = 0;
            Queue.RemoveAll(x => x.GuildId >= 0);
            SongSkipped?.Invoke(Current);
            Current = null;

            if (sendMessage)
            {
                if (tc is not null)
                    await tc.SendMessageAsync("👋");
                else 
                    await TextChannel.SendMessageAsync("👋");
            }

            HasDisconnected = true;
        }

        public Song Pop()
        {
            var song = Queue[0];
            Queue.Remove(song);
            return song;
        }
        public void Dequeue(int id)
        {
            if (id >= 0 && id < Queue.Count)
                Queue.RemoveAt(id);
        }

        public void Shuffle()
        {
            if (Queue.Any())
                Queue = Queue.Shuffle().ToList();
        }

        public async Task UpdateAsync(SocketVoiceChannel channel, SocketTextChannel tC = null)
        {
            VoiceChannel = channel;
            await channel.DisconnectAsync();
            if (tC is not null) TextChannel = tC;
            Connection = await channel.ConnectAsync(true, false);
            Stream = Connection.CreatePCMStream(AudioApplication.Mixed);
            HasDisconnected = false;
        }

        private readonly SemaphoreSlim enq = new SemaphoreSlim(10);
        public async Task EnqueueAsync(SocketTextChannel textChannel, SocketVoiceChannel vc, Song song, bool shuffle = false, int? position = null)
        {
            await enq.WaitAsync();
            try
            {
                Enqueue(song, position);

                if (shuffle) Shuffle();
                
                SongAdded?.Invoke(song);
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
                foreach (var s in songs)
                {
                    Enqueue(s, position);

                    SongAdded?.Invoke(s);
                    if (position.HasValue) position++;
                }

                if (shuffle) Shuffle();
            }
            finally
            {
                enq.Release();
            }
        }

        public async Task ResumeAsync()
        {
            if (!IsPlaying) return;
            if (!IsPaused)
            {
                await TextChannel.SendMessageAsync("Playback has already resumed!");
                return;
            }

            IsPaused = false;
            SongToggledPlaying?.Invoke(IsPaused);
            await TextChannel.SendMessageAsync("Resuming playback.");
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

        //private async Task<Stream> GetAudioAsync(AudioOnlyStreamInfo info) => await yt.Videos.Streams.GetAsync(info);
        private async Task<Stream> GetAudioAsync(AudioOnlyStreamInfo info) => await GetAudioAsync(info.Url);
        private async Task<Stream> GetAudioAsync(string url) => await _hc.GetStreamAsync(url);

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

            return new Song(info, await GetAudioInfoAsync(info.Url), userId, guildId);
        }

        public async Task<Playlist> GetPlaylistAsync(string playlistId, ulong guildId, ulong userId)
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
                    await GetAudioInfoAsync(video.Url),
                    userId,
                    guildId));

            return new Playlist(
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

        private Process InitProcess(string fileName, string args)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            });
        }

        private async Task<YoutubeExplode.Search.VideoSearchResult> GetVideoAsync(string query) => await yt.Search.GetVideosAsync(query).FirstOrDefaultAsync();

        public async Task PauseAsync()
        {
            if (!IsPlaying) return;
            if (IsPaused)
            {
                await TextChannel.SendMessageAsync("Playback is already paused!");
                return;
            }

            IsPaused = true;
            SongToggledPlaying?.Invoke(IsPaused);
            await TextChannel.SendMessageAsync("Pausing playback.");
        }

        internal async Task<int> DoSkipAsync(bool sendMessage = true)
        {
            _skips = 0;
            SongSkipped?.Invoke(Current);

            if (sendMessage)
                await TextChannel.SendMessageAsync("Skipping the current song.");

            return _skips;
        }

        public IEnumerable<SocketGuildUser> GetListeningUsers() => VoiceChannel.Users.Where(x => 
            !x.IsBot && 
            !x.IsDeafened &&
            !x.IsSelfDeafened);

        public async Task<int> SkipAsync(SocketGuildUser skipper)
        {
            if (!IsPlaying)
            {
                await TextChannel.SendMessageAsync("I am not playing anything right now.");
                return _skips;
            }

            var listeningUsers = GetListeningUsers();
            var requiredCount = (int)Math.Floor(.75f * listeningUsers.Count());
            
            if (skipper == VoiceChannel.Guild.CurrentUser) return await DoSkipAsync();
            if (skipper.GuildPermissions.MuteMembers) return await DoSkipAsync();
            if (listeningUsers.Count() == 1 && listeningUsers.First() == skipper)  return await DoSkipAsync();
            if (_skips >= requiredCount)  return await DoSkipAsync();

            if (_skippedUsers.Contains(skipper.Id))
            {
                await TextChannel.SendMessageAsync("You have already voted to skip the current song.");
                return _skips;
            }

            if (skipper.VoiceChannel != VoiceChannel)
            {
                await TextChannel.SendMessageAsync("You are not in the voice channel.");
                return _skips;
            }

            _skips++;
            _skippedUsers.Add(skipper.Id);

            await TextChannel.SendMessageAsync($"Voted to skip the current song. ({_skips} votes/{requiredCount} required)");
            return _skips;
        }

        public async Task AddAsync(SocketGuildUser user, SocketTextChannel channel, string queryOrUrl, bool shuffle = false, int? position = null)
        {
            var id = user.Guild.Id;
            var vc = user.VoiceChannel;
            var uId = user.Id;

            if (vc is null)
            {
                await channel.SendMessageAsync("You must be in a voice channel.");
                return;
            }
            
            if (VoiceChannel is not null && vc != VoiceChannel)
            {
                await channel.SendMessageAsync("You must be in the same voice channel as me.");
                return;
            }

            if (string.IsNullOrWhiteSpace(queryOrUrl))
            {
                await ResumeAsync();
                return;
            }

            var estTime = !Queue.Any() ? "Now" : TimeSpan.FromSeconds(Queue
                .Sum(x => x.Length.TotalSeconds) + (Current?.Length.TotalSeconds - Position.TotalSeconds ?? 0))
                .ToString(@"hh\:mm\:ss");

            if (estTime == "00:00:00" || position == 0) estTime = "Now";

            if (position.HasValue && Queue.Any())
            {
                var len = Queue.Take(position.Value).Sum(x => x.Length.TotalSeconds);

                estTime = TimeSpan.FromSeconds(len +
                    (Current?.Length.TotalSeconds - Position.TotalSeconds ?? 0))
                    .ToString(@"hh\:mm\:ss");
            }

            Discord.Rest.RestUserMessage msg;

            if (Uri.IsWellFormedUriString(queryOrUrl, UriKind.Absolute))
            {

                if (queryOrUrl.Contains("&list=") || queryOrUrl.Contains("?list="))
                {
                    msg = await channel.SendMessageAsync("Adding your playlist...");
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
                        .WithColor(RandomColor())
                        .WithThumbnailUrl(playlist.ThumbnailUrl)
                        .WithCurrentTimestamp()
                        .Build());

                    return;
                }
                else if (queryOrUrl.Contains("spotify.com"))
                {
                    msg = await channel.SendMessageAsync("Adding your playlist...");
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

                    try
                    {
                        await EnqueueManyAsync(channel, vc, playlist, shuffle, position);
                    }
                    finally
                    {
                        await channel.SendMessageAsync(embed: new EmbedBuilder()
                            .WithTitle("Added Spotify playlist")
                            .WithFields(new List<EmbedFieldBuilder>
                            {
                                new EmbedFieldBuilder().WithName("Count").WithValue(spotifyPl.Count).WithIsInline(true),
                                new EmbedFieldBuilder().WithName("URL").WithValue(queryOrUrl).WithIsInline(false),
                                new EmbedFieldBuilder().WithName("Estimated time").WithValue(estTime).WithIsInline(true)
                            })
                            .WithColor(RandomColor())
                            .WithFooter("The songs may sound different as they are coming from YouTube, not Spotify.")
                            .WithCurrentTimestamp()
                            .Build());
                    }

                    return;
                }
            }

            msg = await channel.SendMessageAsync("Adding your song...");

            var song = await CreateSongAsync(queryOrUrl, id, uId);

            if (song is null)
            {
                await channel.SendMessageAsync("Unable to locate a song with that ID or search query; please try again.");
                return;
            }
            
            await msg.DeleteAsync();

            await channel.SendMessageAsync(embed: new EmbedBuilder()
                .WithTitle("Added song")
                .WithFields(new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder().WithName("Title").WithValue(song.Title).WithIsInline(false),
                    new EmbedFieldBuilder().WithName("Author").WithValue(song.Author).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("URL").WithValue(song.Url).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Estimated time").WithValue(estTime).WithIsInline(true)
                })
                .WithColor(RandomColor())
                .WithThumbnailUrl(song.ThumbnailUrl)
                .WithCurrentTimestamp()
            .Build());

            await EnqueueAsync(channel, vc, song, shuffle, position);
        }

        public async Task PlayAsync()
        {
            if (VoiceChannel is null)
                throw new InvalidOperationException("Not connected to a voice channel.");

            if (!Queue.Any())
                return;

            Current = Pop();

            var song = Current;
            
            try
            {
                var botVc = VoiceChannel.Guild.CurrentUser.VoiceChannel;
                if (botVc != VoiceChannel)
                {
                    await botVc.DisconnectAsync();
                    Connection = await VoiceChannel.ConnectAsync(true, false);
                }

                if (IsPlaying)
                    return;

                if (HasDisconnected)
                    await UpdateAsync(VoiceChannel);

                using (var songStream = await GetAudioAsync(song.Info))
                using (var downloadStream = new SimplexStream())
                using (var ffmpeg = InitProcess("ffmpeg", "-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1"))
                using (var input = ffmpeg.StandardInput.BaseStream)
                using (var output = ffmpeg.StandardOutput.BaseStream)
                {
                    IsPlaying = true;

                    var bufferDown = new byte[block_size];
                    var bufferRead = new byte[block_size];
                    var bufferWrite = new byte[block_size];

                    int bytesDown = 0, bytesRead = 0, bytesConverted = 0;

                    var pauseCts = new CancellationTokenSource();

                    SongToggledPlaying += (bool state) =>
                    {
                        if (state)
                            pauseCts.Cancel();
                        else
                            pauseCts = new CancellationTokenSource();

                        return Task.CompletedTask;
                    };

                    async Task Download(CancellationToken token) => await Task.Run(async() =>
                    {
                        try
                        {
                            do
                            {
                                token.ThrowIfCancellationRequested();

                                bytesDown = await songStream.ReadAsync(bufferDown, 0, block_size);
                                BytesDownloaded += (uint)bytesDown;
                                Console.WriteLine(bytesDown);

                                await downloadStream.WriteAsync(bufferDown, 0, bytesDown);
                            }
                            while (bytesDown > 0);
                        }
                        catch (Exception)
                        {

                        }
                        finally
                        {
                            await downloadStream.FlushAsync();
                            downloadStream.CompleteWriting();
                        }
                    });

                    async Task Read(CancellationToken token) => await Task.Run(async() =>
                    {
                        try
                        {
                            do
                            {
                                token.ThrowIfCancellationRequested();

                                bytesRead = await downloadStream.ReadAsync(bufferRead, 0, block_size);
                                await input.WriteAsync(bufferRead, 0, bytesRead);
                            }
                            while (bytesRead > 0);
                        }
                        catch (Exception)
                        {
                            
                        }
                        finally
                        {
                            await input.FlushAsync();
                        }
                    });

                    var hasHadFullChunkYet = false;
                    async Task Write(CancellationToken token) => await Task.Run(async() =>
                    {
                        try
                        {
                            do
                            {
                                while (IsPaused) //don't write to discord while paused
                                {
                                    try
                                    {
                                        token.ThrowIfCancellationRequested();

                                        await Task.Delay(-1, pauseCts.Token); //wait forever (until token is called)
                                    }
                                    catch (TaskCanceledException)
                                    {

                                    }
                                }
                                
                                token.ThrowIfCancellationRequested();

                                if (hasHadFullChunkYet && bytesConverted < block_size) //if bytesConverted is less than here, then the last (small) chunk is done
                                    break;

                                bytesConverted = await output.ReadAsync(bufferWrite, 0, block_size);
                                BytesPlayed += (uint)bytesConverted;

                                if (bytesConverted == block_size)
                                    hasHadFullChunkYet = true;

                                await Stream.WriteAsync(bufferWrite, 0, bytesConverted);
                            }
                            while (bytesConverted > 0);
                        }
                        catch (Exception)
                        {
                            
                        }
                        finally
                        {
                            ffmpeg.Kill();
                        }
                    });

                    try //allows for skipping
                    {
                        var cts = new CancellationTokenSource();

                        SongSkipped += (Song _) =>
                        {
                            if (IsPaused) pauseCts.Cancel();
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
                        await Stream.FlushAsync();

                        if (!ffmpeg.HasExited) 
                            ffmpeg.Kill();
                    }
                }
            }
            catch (Discord.Net.WebSocketClosedException)
            {
                Dispose();
                return;
            }
            finally
            {
                BytesDownloaded = 0;
                BytesPlayed = 0;
                IsPlaying = false;
                SongEnded?.Invoke(song);

                Current = null;
            }
        }

        private Color RandomColor()
        {
            uint clr = Convert.ToUInt32(new Random().Next(0, 0xFFFFFF));
            return new Color(clr);
        }

        #pragma warning disable VSTHRD100 //Avoid "async void" methods, because any exceptions not handled by the method will crash the process. (what else can i do? if i make it return Task the inheritance does not work)
        public async void Dispose()
        {
            HasDisconnected = true;
            IsPlaying = false;
            IsPaused = false;
            await VoiceChannel.DisconnectAsync();
            Connection.Dispose();
            await Stream.DisposeAsync();
        }
        #pragma warning restore VSTHRD100
    }
}
