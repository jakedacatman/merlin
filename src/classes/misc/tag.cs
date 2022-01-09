using LiteDB;

namespace merlin.classes
{
    public class Tag
    {
        public string Value { get; set; }
        public string Key { get; set; }
        public ulong GuildId { get; set; }
        [BsonId]
        public int Id { get; set; }
    }
}