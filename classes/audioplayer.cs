using System;
using Discord.Audio;
using System.Collections.Generic;

namespace donniebot.classes
{
    public class AudioPlayer : IDisposable
    {
        public ulong GuildId { get; }
        public IAudioClient Connection { get; }
        public AudioOutStream Stream { get; set; }
        public List<Song> Queue { get; set; }
        public bool IsPlaying { get; set; } = false;

        public AudioPlayer(ulong id, IAudioClient client)
        {
            GuildId= id;
            Connection = client;
            Stream = client.CreatePCMStream(AudioApplication.Mixed);
            Queue = new List<Song>();
        }

        public void Enqueue(Song s) => Queue.Add(s);
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

        public void UpdateStream() => Stream = Connection.CreatePCMStream(AudioApplication.Mixed);

        public void Dispose()
        {
            IsPlaying = false;
            Connection.Dispose();
            Stream.Dispose();
        }
    }
}