using System.Collections.Generic;
using SixLabors.ImageSharp.Formats;

namespace merlin.classes
{
    public class SvgFormat : IImageFormat
    {
        public string Name => "SVG";
        public string DefaultMimeType => "image/svg+xml";
        public IEnumerable<string> MimeTypes => new[] { "image/svg+xml" };
        public IEnumerable<string> FileExtensions => new[] { "svg" };
    }
}