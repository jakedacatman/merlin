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
        private List<AudioPlayer> _connections = new List<AudioPlayer>();
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

            var connection = await channel.ConnectAsync(true, false);

            var np = new AudioPlayer(id, channel, connection, textChannel, _client, _net);

            if (!GetConnection(id, out var curr))
                _connections.Add(np);
            else
            {
                _connections.Remove(curr);
                _connections.Add(np);
            }

            await textChannel.SendMessageAsync($"Joined `{channel.Name}` and will send messages in `#{textChannel.Name}`.");

            return np;
        }

        public async Task DisconnectAsync(ISocketMessageChannel tc = null)
        {
            if (GetConnection((tc as SocketGuildChannel).Guild.Id, out var connection))
                await connection.DisconnectAsync(tc);
        }

        public async Task AddAsync(SocketGuildUser user, SocketTextChannel channel, string queryOrUrl, bool shuffle = false, int? position = null)
        {
            var id = user.Guild.Id;
            var vc = user.VoiceChannel;
            var uId = user.Id;

            if (vc == null)
            {
                await channel.SendMessageAsync("You must be in a voice channel.");
                return;
            }
            
            if (GetConnection(id, out var con) && vc != con.VoiceChannel)
            {
                await channel.SendMessageAsync("You must be in the same voice channel as me.");
                return;
            }

            if (con is null)
                con = await ConnectAsync(channel, vc);

            await con.AddAsync(user, channel, queryOrUrl, shuffle, position);
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
            var id = vc.Guild.Id;
            if (!GetConnection(id, out var player))
                player = await ConnectAsync(textChannel, vc);

            await player.EnqueueAsync(textChannel, vc, song, shuffle, position);
        }
        public async Task EnqueueManyAsync(SocketTextChannel textChannel, SocketVoiceChannel vc, IEnumerable<Song> songs, bool shuffle = false, int? position = null)
        {
            var id = vc.Guild.Id;
            if (!GetConnection(id, out var player))
                player = await ConnectAsync(textChannel, vc);

            await player.EnqueueManyAsync(textChannel, vc, songs, shuffle, position);
        }

        public bool ToggleLoop(ulong id)
        {
            if (!GetConnection(id, out var c))
                return false;

            c.ToggleLoop();
            return c.IsLooping;
        }

        public bool IsLooping(ulong id)
        {
            if (!GetConnection(id, out var c))
                return false;

            return c.IsLooping;
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

        public bool ClearQueue(ulong id)
        {
            if (!GetConnection(id, out var c))
                return false;

            var queue = c.Queue;

            queue.RemoveRange(0, queue.Count);
            return true;
        }

        public void Shuffle(ulong id)
        {
            if (!GetConnection(id, out var c))
                return;

            c.Shuffle();
        }

        public async Task PlayAsync(ulong id)
        {
            if (!GetConnection(id, out var connection))
                throw new InvalidOperationException("Not connected to a voice channel.");
            
            await connection.PlayAsync();
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
                    items.Add($"{(connection.IsPlaying && !connection.IsPaused ? "▶️" : "⏸️")} __**1**: {connection.Current.Title} (queued by {GetMention(gId, connection.Current.QueuerId)})__");

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
