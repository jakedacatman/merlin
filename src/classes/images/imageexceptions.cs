using System;

namespace donniebot.classes
{
    public class VideoException : Exception
    {
        public VideoException() { }
        public VideoException(string message) : base(message) { }
        public VideoException(string message, Exception inner) : base(message, inner) { }
    }

    public class ImageException : Exception
    {
        public ImageException() { }
        public ImageException(string message) : base(message) { }
        public ImageException(string message, Exception inner) : base(message, inner) { }
    }
}