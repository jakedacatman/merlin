using LiteDB;

namespace donniebot.classes
{
    public class Host
    {
        public string Name { get; set; }
        public string Url { get; set; }
        [BsonId]
        public int Id { get; set; }
    }
}