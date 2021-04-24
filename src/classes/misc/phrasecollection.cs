using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;
using System.IO;

namespace donniebot.classes
{
    public class PhraseCollection : List<string>
    {
        [BsonId]
        public string Name { get; set; }

        public PhraseCollection(string name)
        {
            Name = name;
        }

        public static PhraseCollection Load(string fileName, string name)
        {
            var pc = new PhraseCollection(name);
            if (!File.Exists(fileName))
                return pc;
            
            var lines = File.ReadAllLines(fileName);
            pc.AddRange(lines);
            return pc;
        }

        public async Task SaveAsync(string fileName, PhraseCollection list)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            await File.WriteAllLinesAsync(fileName, list);
        }
    }
}