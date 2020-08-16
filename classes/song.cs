using System;

namespace donniebot.classes
{
    public class Song
    {
        public string Title { get; }
        public string Url { get; }
        public ulong QueuerId { get; }
        public ulong GuildId { get; }

        public Song(string title, string url, ulong queuerId, ulong guildId)
        {
            Title = title;
            Url = url;
            QueuerId = queuerId;
            GuildId = guildId;
        }
    }
}