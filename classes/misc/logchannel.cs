using LiteDB;

namespace donniebot.classes
{
    public class LogChannel
    {
        public LogLevel Level { get; set; }
        [BsonId]
        public ulong Id { get; set; }
    }

    public enum LogLevel
    {
        None,
        Edits,
        Deletions,
        All
    }
}