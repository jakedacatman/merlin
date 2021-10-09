using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;
using System.IO;

namespace donniebot.classes
{
    public class SubredditCollection : List<string>
    {
        [BsonId]
        public string Name { get; set; }

        public SubredditCollection(string name)
        {
            Name = name;
        }

        public static SubredditCollection Load(string fileName, string name)
        {
            var sl = new SubredditCollection(name);
            if (!File.Exists(fileName))
                return sl;
            
            var lines = File.ReadAllLines(fileName);
            sl.AddRange(lines);
            return sl;
        }

        public async Task SaveAsync(string fileName, SubredditCollection list)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            await File.WriteAllLinesAsync(fileName, list);
        }
    }
}