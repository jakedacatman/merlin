using System;

namespace donniebot.classes
{
    public class SongInfo
    {
        public string Title { get; }
        public string Url { get; }
        public string ThumbnailUrl { get; }
        public string Author { get; }

        public SongInfo(string title, string url, string thumbnail, string author)
        {
            Title = title;
            Url = url;
            ThumbnailUrl = thumbnail;
            Author = author;
        }
    }
}