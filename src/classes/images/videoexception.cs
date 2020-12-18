using System;

namespace donniebot.classes
{
    public class VideoException : Exception
    {
        public VideoException() { }
        public VideoException(string message) : base(message) { }
        public VideoException(string message, Exception inner) : base(message, inner) { }
    }
}