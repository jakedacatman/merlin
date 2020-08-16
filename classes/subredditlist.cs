using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LiteDB;
using System.IO;

namespace donniebot.classes
{
    public class SubredditCollection : Collection<string>
    {
        [BsonId]
        public string Name { get; set; }

        public SubredditCollection(string name)
        {
            Name = name;
        }

        public static SubredditCollection Load(string fileName, string name)
        {
            if (!File.Exists(fileName))
                return null;
            
            var lines = File.ReadAllLines(fileName);

            var sl = new SubredditCollection(name);

            foreach (var line in lines)
                sl.Add(line);

            return sl;
        }

        public async Task Save(string fileName, SubredditCollection list)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            await File.WriteAllLinesAsync(fileName, list);
        }
    }
}