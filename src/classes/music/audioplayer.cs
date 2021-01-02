using System;
using System.Linq;
using Discord.Audio;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace donniebot.classes
{
    public class AudioPlayer : IDisposable
    {
        public ulong GuildId { get; }
        public IAudioClient Connection { get; private set; }
        public SocketVoiceChannel VoiceChannel { get; private set; }
        public SocketTextChannel TextChannel { get; }
        public AudioOutStream Stream { get; private set; }
        public Song Current { get; set; } = null;
        public List<Song> Queue { get; set; }
        public bool HasDisconnected { get; private set; } = false;
        public bool IsPlaying { get; set; } = false;
        public bool IsPaused { get; private set; } = false;
        private int _skips = 0;
        private List<ulong> _skippedUsers = new List<ulong>();

        public event Func<AudioPlayer, Song, Task> SongSkipped;
        public event Func<AudioPlayer, bool, Task> SongPaused;

        public AudioPlayer(ulong id, SocketVoiceChannel channel, IAudioClient client, SocketTextChannel textchannel)
        {
            GuildId= id;
            VoiceChannel = channel;
            Connection = client;
            TextChannel = textchannel;
            Stream = client.CreatePCMStream(AudioApplication.Mixed);
            Queue = new List<Song>();
        }

        public void Enqueue(Song s, int? position)
        { 
            if (!position.HasValue)
                Queue.Add(s);
            else
                Queue.Insert(position.Value, s);
        }

        public void EnqueueMany(IEnumerable<Song> s, int? position)
        {
            if (!position.HasValue)
                Queue.AddRange(s);
            else
                Queue.InsertRange(position.Value, s);
        }

        public async Task LeaveAsync()
        {
            _skips = 0;
            Queue.RemoveAll(x => x.GuildId >= 0);
            SongSkipped?.Invoke(this, this.Current);
            await TextChannel.SendMessageAsync("👋");
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

        public async Task UpdateAsync(SocketVoiceChannel channel)
        {
            VoiceChannel = channel;
            Connection = await channel.ConnectAsync(true, false);
            Stream = Connection.CreatePCMStream(AudioApplication.Mixed);
            HasDisconnected = false;
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
            SongPaused?.Invoke(this, IsPaused);
            await TextChannel.SendMessageAsync("Resuming playback.");
        }

        public async Task PauseAsync()
        {
            if (!IsPlaying) return;
            if (IsPaused)
            {
                await TextChannel.SendMessageAsync("Playback is already paused!");
                return;
            }

            IsPaused = true;
            SongPaused?.Invoke(this, IsPaused);
            await TextChannel.SendMessageAsync("Pausing playback.");
        }

        private async Task<int> DoSkipAsync()
        {
            _skips = 0;
            SongSkipped?.Invoke(this, this.Current);
            await TextChannel.SendMessageAsync("Skipping the current song.");
            return 0;
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

        public async void Dispose()
        {
            HasDisconnected = true;
            IsPlaying = false;
            IsPaused = false;
            await VoiceChannel.DisconnectAsync();
            Connection.Dispose();
            await Stream.DisposeAsync();
        }
    }
}