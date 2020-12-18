using System;

namespace donniebot.classes
{
    public class ImageException : Exception
    {
        public ImageException() { }
        public ImageException(string message) : base(message) { }
        public ImageException(string message, Exception inner) : base(message, inner) { }
    }
}