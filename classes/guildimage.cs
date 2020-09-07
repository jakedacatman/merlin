namespace donniebot.classes
{
    public class GuildImage
    {
        public string Url { get; }
        public ulong GuildId { get; }
        public string SourceUrl { get; } = null;
        public string Author { get; } = null;
        public string Subreddit { get; set; } = null;
        public string Title { get; } = null;
        public string Type { get; } = null;

        public GuildImage(string url, ulong gId, string source = null, string author = null, string sub = null, string title = null, string type = null)
        {
            Url = url;
            GuildId = gId;
            SourceUrl = source;
            Author = author;
            Subreddit = sub;
            Title = title;
            Type = type;
        }

        public static bool IsEqual(GuildImage orig, GuildImage comp) => (orig.GuildId == comp.GuildId) && (orig.Url == comp.Url) && (orig.SourceUrl == comp.SourceUrl);
    }
}