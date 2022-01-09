using LiteDB;

namespace merlin.classes
{
    public class GuildPrefix
    {
        public ulong GuildId { get; set; }
        public string Prefix { get; set; }
        [BsonId]
        public int Id { get; set; }
    }
}