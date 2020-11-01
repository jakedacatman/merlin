using System;
using System.Linq;
using Discord.Audio;
using Discord.WebSocket;
using System.Collections.Generic;

namespace donniebot.classes
{
    public class AudioPlayer : IDisposable
    {
        public ulong GuildId { get; }
        public IAudioClient Connection { get; }
        public SocketVoiceChannel Channel { get; }
        public AudioOutStream Stream { get; set; }
        public Song Current { get; set; } = null;
        public List<Song> Queue { get; set; }
        public bool IsPlaying { get; set; } = false;
        public bool IsSkipping { get; set; } = false;
        private int _skips = 0; 

        public AudioPlayer(ulong id, SocketVoiceChannel channel, IAudioClient client)
        {
            GuildId= id;
            Channel = channel;
            Connection = client;
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

        public int Skip(SocketGuildUser skipper)
        {
            if (!IsPlaying)
                return 0;

            var listeningUsers = Channel.Users.Where(x => 
                !x.IsBot && 
                !x.IsDeafened &&
                !x.IsSelfDeafened);

            if (skipper.GuildPermissions.MuteMembers)
            {
                _skips = 0;
                IsSkipping = true;
                return 0;
            }

            if (listeningUsers.Count() == 1 && listeningUsers.First() == skipper)
            {
                _skips = 0;
                IsSkipping = true;
                return 0;
            }

            if (_skips >= (int)Math.Floor(.75f * listeningUsers.Count()))
            {
                _skips = 0;
                IsSkipping = true;
                return 0;
            }

            _skips++;
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