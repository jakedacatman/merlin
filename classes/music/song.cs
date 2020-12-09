using System;
using YoutubeExplode.Videos.Streams;

namespace donniebot.classes
{
    public class Song
    {
        public string Author { get; }
        public string Title { get; }
        public string Url { get; }
        public string ThumbnailUrl { get; }
        public ulong QueuerId { get; }
        public ulong GuildId { get; }
        public TimeSpan Length { get; }
        public long Size { get; set; } = 0L;
        public AudioOnlyStreamInfo Info { get; set; } = null;

        public Song(SongInfo info, ulong queuerId, ulong guildId)
        {
            Author = info.Author;
            Title = info.Title;
            Url = info.Url;
            ThumbnailUrl = info.ThumbnailUrl;
            QueuerId = queuerId;
            GuildId = guildId;
            Length = info.Length;
        }
    }
}