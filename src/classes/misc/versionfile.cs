using System;

namespace donniebot.classes
{
    public class VersionFile
    {
        public string Commit { get; set; }
        public string Author { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
    }
}