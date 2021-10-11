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
using System.Text.RegularExpressions;
using System.Net.Http;

namespace donniebot.services
{
    public class AudioService
    {
        private List<AudioPlayer> _players = new List<AudioPlayer>();
        private readonly DiscordShardedClient _client;
        private readonly NetService _net;
        private readonly DbService _db;
        private readonly RandomService _rand;
        private YoutubeClient yt = new YoutubeClient();
        private HttpClient _hc = new HttpClient();
        private Dictionary<ulong, CancellationTokenSource> queueEndTokens = new Dictionary<ulong, CancellationTokenSource>();

        public AudioService(DiscordShardedClient client, NetService net, DbService db, RandomService rand)
        {
            _client = client;
            _net = net;
            _db = db;
            _rand = rand;
        }

        public async Task<AudioPlayer> ConnectAsync(SocketTextChannel textChannel, SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;
            var currUser = channel.Guild.CurrentUser;

            if (currUser.VoiceChannel != null)
                await channel.DisconnectAsync();

            AudioPlayer a;

            if (!GetPlayer(id, out var curr))
            {
                var connection = await channel.ConnectAsync(true, false);
                a = new AudioPlayer(id, channel, connection, textChannel, _client, _net);
                _players.Add(a);
            }
            else
            {
                await curr.UpdateAsync(channel, textChannel);
                a = curr;
            }

            await textChannel.SendMessageAsync($"Joined `{channel.Name}` and will send messages in `#{textChannel.Name}`.");

            return a;
        }

        public async Task DisconnectAsync(ISocketMessageChannel tc = null)
        {
            if (GetPlayer((tc as SocketGuildChannel).Guild.Id, out var player))
                await player.DisconnectAsync(tc);
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
            
            if (GetPlayer(id, out var player) && vc != player.VoiceChannel)
            {
                await channel.SendMessageAsync("You must be in the same voice channel as me.");
                return;
            }

            if (player is null)
                player = await ConnectAsync(channel, vc);

            await player.AddAsync(user, channel, queryOrUrl, shuffle, position);
        }

        public async Task ResumeAsync(ulong guildId)
        {
            if (!GetPlayer(guildId, out var player)) return;
            await player.ResumeAsync();
        }

        public async Task PauseAsync(ulong guildId)
        {
            if (!GetPlayer(guildId, out var player)) return;
            await player.PauseAsync();
        }

        private readonly SemaphoreSlim enq = new SemaphoreSlim(1, 1);
        public async Task EnqueueAsync(SocketTextChannel textChannel, SocketVoiceChannel vc, Song song, bool shuffle = false, int? position = null)
        {
            var id = vc.Guild.Id;
            if (!GetPlayer(id, out var player))
                player = await ConnectAsync(textChannel, vc);

            await player.EnqueueAsync(textChannel, vc, song, shuffle, position);
        }
        public async Task EnqueueManyAsync(SocketTextChannel textChannel, SocketVoiceChannel vc, IEnumerable<Song> songs, bool shuffle = false, int? position = null)
        {
            var id = vc.Guild.Id;
            if (!GetPlayer(id, out var player))
                player = await ConnectAsync(textChannel, vc);

            await player.EnqueueManyAsync(textChannel, vc, songs, shuffle, position);
        }

        public bool ToggleLoop(ulong id)
        {
            if (!GetPlayer(id, out var player))
                return false;

            player.ToggleLoop();
            return player.IsLooping;
        }

        public bool IsLooping(ulong id)
        {
            if (!GetPlayer(id, out var player))
                return false;

            return player.IsLooping;
        }

        public bool RemoveAt(ulong id, int index)
        {
            if (!GetPlayer(id, out var player))
                return false;

            var queue = player.Queue;

            if (index < 1 || index >= queue.Count)
                return false;

            queue.RemoveAt(index);
            return true;
        }

        public bool ClearQueue(ulong id)
        {
            if (!GetPlayer(id, out var player))
                return false;

            var queue = player.Queue;

            queue.RemoveRange(0, queue.Count);
            return true;
        }

        public void Shuffle(ulong id)
        {
            if (!GetPlayer(id, out var player))
                return;

            player.Shuffle();
        }

        public async Task PlayAsync(ulong id)
        {
            if (!GetPlayer(id, out var player))
                throw new InvalidOperationException("Not connected to a voice channel.");
            
            await player.PlayAsync();
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

            return new Song(info, await GetAudioInfoAsync(info.Url) ,userId, guildId);
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
                    await GetAudioInfoAsync(video.Url),
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

        public bool IsConnected(ulong id) => GetPlayer(id, out var _);

        public bool HasSongs(ulong id)
        {
            if (!GetPlayer(id, out var player)) return false; 
            var hasCurrent = player.Current != null;
            var hasSongsInQueue = player.Queue.Any();

            return hasCurrent || hasSongsInQueue;
        }

        public async Task<int> SkipAsync(SocketGuildUser skipper)
        {
            if (GetPlayer(skipper.Guild.Id, out var player))
            {
                var djRole = _db.GetItem<DjRole>("djroles", LiteDB.Query.EQ("GuildId", skipper.Guild.Id));
                if (skipper.Roles.Any(x => x.Id == djRole?.RoleId))
                    return await player.DoSkipAsync();

                return await player.SkipAsync(skipper);
            }
            else return 0;
        }

        public string GetSongPosition(ulong id) => GetPlayer(id, out var player) ? $"{player.Position.ToString(@"hh\:mm\:ss")}/{player.Current.Length.ToString(@"hh\:mm\:ss")}" : null;
        public TimeSpan GetRawPosition(ulong id) => GetPlayer(id, out var player) ? player.Current?.Length - player.Position ?? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(0);
        public double GetDownloadedPercent(ulong id) => GetPlayer(id, out var player) ? (double)player.BytesDownloaded / player.Current.Size : 0d;

        public List<SocketGuildUser> GetListeningUsers(ulong id) => GetPlayer(id, out var player) ? player.GetListeningUsers().ToList() : new List<SocketGuildUser>();

        public Song GetCurrent(ulong id) => GetPlayer(id, out var player) ? player.Current : null;

        public List<string> GetQueue(ulong id)
        {
            var items = new List<string>();

            if (GetPlayer(id, out var player))
            {
                var queue = player.Queue;
                var gId = player.GuildId;

                if (player.Current is not null)
                {
                    items.Add($"__**1**: {player.Current.Title}__ ({Math.Round(GetDownloadedPercent(gId) * 100d, 1)}%) {(player.IsPlaying && !player.IsPaused ? "▶️" : "⏸️")}");

                    for (int i = 0; i < queue.Count; i++)
                        items.Add($"**{i + 2}**: {queue[i].Title} (queued by {GetMention(player.GuildId, queue[i].QueuerId)})");
                }
                else
                    for (int i = 0; i < queue.Count; i++)
                        items.Add($"**{i + 1}**: {queue[i].Title} (queued by {GetMention(player.GuildId, queue[i].QueuerId)})");
            }

            return items;
        }
        public List<Song> GetRawQueue(ulong id) => GetPlayer(id, out var player) ? player.Queue : new List<Song>();

        private string GetMention(ulong guildId, ulong userId) =>
            _client
                .GetGuild(guildId)
                .GetUser(userId)
                .Mention;

        public bool GetPlayer(ulong id, out AudioPlayer player)
        {
            player = null;
            var exists = _players.Any(x => x.GuildId == id);

            if (exists)
                player = _players.First(x => x.GuildId == id);

            return exists;
        }
    }
}
