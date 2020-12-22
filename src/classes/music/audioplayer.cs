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
        public IAudioClient Connection { get; }
        public SocketVoiceChannel VoiceChannel { get; }
        public SocketTextChannel TextChannel { get; }
        public AudioOutStream Stream { get; set; }
        public Song Current { get; set; } = null;
        public List<Song> Queue { get; set; }
        public bool IsPlaying { get; set; } = false;
        public bool IsSkipping { get; set; } = false;
        private int _skips = 0;
        private List<ulong> _skippedUsers = new List<ulong>();

        public event Func<AudioPlayer, Song, Task> SongSkipped;

        public AudioPlayer(ulong id, SocketVoiceChannel channel, IAudioClient client, SocketTextChannel textchannel)
        {
            GuildId= id;
            VoiceChannel = channel;
            Connection = client;
            TextChannel = textchannel;
            Stream = client.CreatePCMStream(AudioApplication.Mixed);
            Queue = new List<Song>();
        }

        public void Enqueue(Song s) => Queue.Add(s);

        public void EnqueueMany(IEnumerable<Song> s) => Queue.AddRange(s);

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

        public void UpdateStream() => Stream = Connection.CreatePCMStream(AudioApplication.Mixed);

        private async Task<int> DoSkipAsync()
        {
            
            _skips = 0;
            IsSkipping = true;
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
                return 0;
            }

            var listeningUsers = GetListeningUsers();

            var requiredCount = (int)Math.Floor(.75f * listeningUsers.Count());
            if (skipper.GuildPermissions.MuteMembers) return await DoSkipAsync();
            if (listeningUsers.Count() == 1 && listeningUsers.First() == skipper)  return await DoSkipAsync();
            if (_skips >= requiredCount)  return await DoSkipAsync();

            if (_skippedUsers.Contains(skipper.Id))
            {
                await TextChannel.SendMessageAsync("You have already voted to skip the current song.");
                return 0;
            }

            if (skipper.VoiceChannel != VoiceChannel)
            {
                await TextChannel.SendMessageAsync("You are not in the voice channel.");
                return 0;
            }

            _skips++;
            _skippedUsers.Add(skipper.Id);

            await TextChannel.SendMessageAsync($"Voted to skip the current song. ({_skips} votes/{requiredCount} required)");
            return _skips;
        }

        public void Dispose()
        {
            IsPlaying = false;
            IsSkipping = false;
            Connection.Dispose();
            Stream.Dispose();
        }
    }
}