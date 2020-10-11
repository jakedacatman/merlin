using System;
using Discord.Audio;

namespace donniebot.classes
{
    public class AudioPlayer : IDisposable
    {
        public ulong GuildId { get; }
        public IAudioClient Connection { get; }
        public AudioOutStream Stream { get; set; }

        public AudioPlayer(ulong id, IAudioClient client)
        {
            GuildId= id;
            Connection = client;
            Stream = client.CreatePCMStream(AudioApplication.Mixed);
        }

        public void UpdateStream()
        {
            Stream = Connection.CreatePCMStream(AudioApplication.Mixed);
        }

        public void Dispose()
        {
            Connection.Dispose();
            Stream.Dispose();
        }
    }
}