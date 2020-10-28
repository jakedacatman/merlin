namespace donniebot.classes
{
    public class ImageProperties
    {
        public int Width { get; }
        public int Height { get; }
        public int Frames { get; }
        public int BitsPerPixel { get; }
        public double FPS { get; }
        public string MimeType { get; }
        public double HorizontalResolution { get; }
        public double VerticalResolution { get; }
        
        public ImageProperties(int width, int height, int frames, int bpp, double fps, string mime, double h, double v)
        {
            Width = width;
            Height = height;
            Frames = frames;
            BitsPerPixel = bpp;
            FPS = fps;
            MimeType = mime;
            HorizontalResolution = h;
            VerticalResolution = v;
        }
    }
}