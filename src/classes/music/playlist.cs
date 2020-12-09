using System;
using System.Collections.Generic;

namespace donniebot.classes
{
    public class Playlist
    {
        public List<Song> Songs { get; }
        public string Title { get; }
        public string Author { get; }
        public string Url { get; }
        public string ThumbnailUrl { get; }
        public ulong QueuerId { get; }
        public ulong GuildId { get; }

        public Playlist(List<Song> songs, string title, string author, string url, string thumbnailUrl, ulong queuerId, ulong guildId)
        {
            Songs = songs;
            Title = title;
            Author = author;
            Url = url;
            ThumbnailUrl = thumbnailUrl;
            QueuerId = queuerId;
            GuildId = guildId;
        }
    }
}