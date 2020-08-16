using System;

namespace donniebot.classes
{
    public class GuildImage
    {
        public string Url { get; }
        public ulong GuildId { get; }

        public GuildImage(string url, ulong gId)
        {
            Url = url;
            GuildId = gId;
        }

        public static bool IsEqual(GuildImage orig, GuildImage comp) => (orig.GuildId == comp.GuildId) && (orig.Url == comp.Url);
    }
}