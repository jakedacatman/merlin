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
        public long Size 
        { 
            get => Info?.Size.Bytes ?? 0;
        }
        public AudioOnlyStreamInfo Info { get; }

        public Song(SongInfo info, AudioOnlyStreamInfo audioInfo, ulong queuerId, ulong guildId)
        {
            Author = info.Author;
            Title = info.Title;
            Url = info.Url;
            ThumbnailUrl = info.ThumbnailUrl;
            Info = audioInfo;
            QueuerId = queuerId;
            GuildId = guildId;
            Length = info.Length;
        }
    }
}