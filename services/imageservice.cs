using System;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Collections.Generic;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using donniebot.classes;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using Discord.WebSocket;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace donniebot.services
{
    public class ImageService
    {
        private readonly MiscService _misc;
        private readonly NetService _net;
        private readonly RandomService _rand;

        private IImageFormat _format;
        private List<GuildImage> sentImages = new List<GuildImage>();
        private readonly Regex _reg = new Regex(@"[0-9]+(\.[0-9]{1,2})? fps");
        private readonly NekoEndpoints _nkeps;

        public ImageService(MiscService misc, NetService net, RandomService rand, NekoEndpoints nkeps)
        {
            _misc = misc;
            _net = net;
            _rand = rand;
            _nkeps = nkeps;
        }

        public async Task<Image> Invert(string url) => Invert(await DownloadFromUrlAsync(url));
        public Image Invert(Image source)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, Invert);
            else
                source.Mutate(x => x.Invert());
            return source;
        }

        public async Task<Image> Brightness(string url, float brightness) => Brightness(await DownloadFromUrlAsync(url), brightness);
        public Image Brightness(Image source, float brightness)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, brightness, Brightness);
            else
                source.Mutate(x => x.Brightness(brightness));
            return source;
        }

        public async Task<Image> Blur(string url, float amount) => Blur(await DownloadFromUrlAsync(url), amount);
        public Image Blur(Image source, float amount)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, amount, Blur);
            else
                source.Mutate(x => x.GaussianBlur(amount));
            return source;
        }

        public async Task<Image> Greyscale(string url) => Greyscale(await DownloadFromUrlAsync(url));
        public Image Greyscale(Image source)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, Greyscale);
            else
                source.Mutate(x => x.Grayscale());
            return source;
        }

        public async Task<Image> Edges(string url) => Edges(await DownloadFromUrlAsync(url));
        public Image Edges(Image source)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, Edges);
            else
                source.Mutate(x => x.DetectEdges());
            return source;
        }

        public async Task<Image> Contrast(string url, float amount) => Contrast(await DownloadFromUrlAsync(url), amount);
        public Image Contrast(Image source, float amount)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, amount, Contrast);
            else
                source.Mutate(x => x.Contrast(amount));
            return source;
        }

        public async Task<Image> Sharpen(string url, float amount) => Sharpen(await DownloadFromUrlAsync(url), amount);
        public Image Sharpen(Image source, float amount)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, amount, Sharpen);
            else
                source.Mutate(x => x.GaussianSharpen(amount));
            return source;
        }

        public async Task<Image> Pixelate(string url, int size) => Pixelate(await DownloadFromUrlAsync(url), size);
        public Image Pixelate(Image source, int size)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, size, Pixelate);
            else
                source.Mutate(x => x.Pixelate(size));
            return source;
        }
        
        public async Task<Image> Hue(string url, float amount) => Hue(await DownloadFromUrlAsync(url), amount);

        public Image Hue(Image source, float amount)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, amount, Hue);
            else
                source.Mutate(x => x.Hue(amount));
            return source;
        }

        public async Task<Image> BackgroundColor(string url, int r, int g, int b) => BackgroundColor(await DownloadFromUrlAsync(url), r, g, b);
        public Image BackgroundColor(Image source, int r, int g, int b)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, r, g, b, BackgroundColor);
            else
                source.Mutate(x => x.BackgroundColor(new Color(new Rgba64((ushort)r, (ushort)g, (ushort)b, 255))));
            return source;
        }

        public async Task<Image> Rotate(string url, float r) => Rotate(await DownloadFromUrlAsync(url), r);
        public Image Rotate(Image source, float r)
        {
            if (source.Frames.Count > 1)
                return GifFilterR(source, r, Rotate);
            else
                source.Mutate(x => x.Rotate(r));
            return source;
        }

        public async Task<Image> Caption(string url, string text) => Caption(await DownloadFromUrlAsync(url), text);
        public Image Caption(Image source, string text)
        {
            var img = Image.Load(new byte[]
            {
                137, 80, 78, 71, 13, 
                10, 26, 10, 0, 0, 
                0, 13, 73, 72, 68, 
                82, 0, 0, 0, 1, 
                0, 0, 0, 1, 8, 
                6, 0, 0, 0, 31, 
                21, 196, 137, 0, 0, 
                0, 11, 73, 68, 65, 
                84, 8, 215, 99, 248, 
                15, 4, 0, 9, 251, 
                3, 253, 99, 38, 197, 
                143, 0, 0, 0, 0, 
                73, 69, 78, 68, 174, 
                66, 96, 130
            });

            var font = SystemFonts.Collection.CreateFont("LimerickCdSerial-Xbold", source.Width / 12f, FontStyle.Bold);
            //var font = SystemFonts.Collection.CreateFont("Twemoji", source.Width / 12f, FontStyle.Regular);
            float padding = 0.05f * source.Width;
            float wrap = source.Width - (2 * padding);

            var bounds = TextMeasurer.Measure(text, new RendererOptions(font) 
            { 
                WrappingWidth = wrap, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center 
            });

            var height = Math.Max((int)(bounds.Height * 1.25), (int)(source.Height / 4.5f));
            
            img.Mutate(x => x.Resize(source.Width, height + source.Height));

            var to = new TextOptions
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                WrapTextWidth = wrap,
                FallbackFonts = 
                {
                    SystemFonts.Find("Twemoji")
                    //SystemFonts.Find("LimerickCdSerial-Xbold")
                }
            };

            var options = new TextGraphicsOptions(new GraphicsOptions(), to);

            PointF location = new PointF(padding, height * .5f);

            img.Mutate(x => x.DrawText(options, text, font, Color.Black, location));

            if (source.Frames.Count() > 1)
            {
                var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
                for (int i = 1; i < source.Frames.Count; i++)
                {
                    var frame = img.Frames.CloneFrame(0).Frames[0];
                    frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                    img.Frames.InsertFrame(i, frame);
                }
                        
                return GifFilter((Image)img, source, new Point(0, height), source.Size(), 0f, Overlay);
            }
            else return Overlay((Image)img, source, new Point(0, height), source.Size());
        }

        public async Task<Image> Overlay(string sourceUrl, string overlayUrl, int x, int y, int width, int height, float rot = 0f)
        {
            Image source = await DownloadFromUrlAsync(sourceUrl);
            var overlay = await DownloadFromUrlAsync(overlayUrl);

            if (width == 0) width = overlay.Width;
            if (height == 0) height = overlay.Height;

            if (width < 0) width = source.Width;
            if (height < 0) height = source.Height;

            var size = new Size(width, height);

            Point location;
            if (x == -1 && y == -1)
                location = new Point((source.Width / 2) - (size.Width / 2), (source.Height / 2) - (size.Height / 2));
            else
                location = new Point(x, y);

            return Overlay(source, overlay, location, size, rot);  
        }
        public Image Overlay(Image source, Image overlay, Point location, Size size, float rot = 0f)
        {
            if (rot != 0f)
            {
                var ow = overlay.Width;
                var oh = overlay.Height;
                overlay.Mutate(x => x.Rotate(rot));
                var nw = overlay.Width;
                var nh = overlay.Height;
            
                location = new Point(location.X - ((nw - ow) / 2), location.Y - ((nh - oh) / 2));
            }

            source.Mutate(x => x.DrawImage(overlay, location, 1f));
            return source;
        }

        public async Task<Image> Saturate(string url, float amount) => Saturate(await DownloadFromUrlAsync(url), amount);
        public Image Saturate(Image source, float amount)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, amount, Saturate);
            else
                source.Mutate(x => x.Saturate(amount));
            return source;
        }

        public async Task<Image> Glow(string url) => Glow(await DownloadFromUrlAsync(url));
        public Image Glow(Image source)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, Glow);
            else
                source.Mutate(x => x.Glow());
            return source;
        }

        public async Task<Image> Polaroid(string url) => Polaroid(await DownloadFromUrlAsync(url));
        public Image Polaroid(Image source)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, Polaroid);
            else
                source.Mutate(x => x.Polaroid());
            return source;
        }

        public async Task<Image> Jpeg(string url, int quality)=> Jpeg(await DownloadFromUrlAsync(url), quality);
        public Image Jpeg(Image source, int quality)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, quality, Jpeg);
            else
            {
                var path = SaveAsJpeg(source, quality);
                var f = File.Open(path, FileMode.Open);
                source = Image.Load(f);
                f.Dispose();
                File.Delete(path);  
            }
            return source;
        }

        public async Task<Image> Demotivational(string url, string title, string text) => Demotivational(await DownloadFromUrlAsync(url), title, text);
        public Image Demotivational(Image source, string title, string text)
        {
            Image bg = Image.Load(new byte[]
            {
                137, 80, 78, 71, 13, 
                10, 26, 10, 0, 0, 
                0, 13, 73, 72, 68, 
                82, 0, 0, 0, 1, 
                0, 0, 0, 1, 8, 
                6, 0, 0, 0, 31, 
                21, 196, 137, 0, 0, 
                0, 6, 98, 75, 71, 
                68, 0, 104, 0, 137, 
                0, 128, 83, 93, 36, 
                220, 0, 0, 0, 13, 
                73, 68, 65, 84, 8, 
                215, 99, 96, 100, 96, 
                252, 15, 0, 1, 10, 
                1, 2, 233, 211, 149, 
                8, 0, 0, 0, 0, 
                73, 69, 78, 68, 174, 
                66, 96, 130
            });

            var w = source.Width;
            var h = source.Height;

            var font = SystemFonts.Collection.CreateFont("Linux Libertine", source.Width / 12f, FontStyle.Regular);
            var tFont = SystemFonts.Collection.CreateFont("Linux Libertine", source.Width / 6f, FontStyle.Regular);
            float padding = 0.05f * source.Width;
            float wrap = source.Width - (2 * padding);

            TextMeasurer.TryMeasureCharacterBounds(text, new RendererOptions(font) 
            { 
                WrappingWidth = wrap, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center 
            }, out var bnds);

            var bounds = bnds.Any() ? bnds.OrderByDescending(x => x.Bounds.Height).Select(x => x.Bounds).First() : new FontRectangle(0, 0, 0, 0);
            
            TextMeasurer.TryMeasureCharacterBounds(title, new RendererOptions(tFont) 
            { 
                WrappingWidth = wrap, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center 
            }, out var tbnds);

            var tBounds = tbnds.Any() ?  tbnds.OrderByDescending(x => x.Bounds.Height).Select(x => x.Bounds).First() : new FontRectangle(0, 0, 0, 0);

            var bw = (int)Math.Round(w / 8d);
            var bh = (int)Math.Round(h / 8d);

            var height = (int)tBounds.Height + bh;

            bg.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Stretch,
                Size = new Size((int)Math.Round(5d * w / 4d), (int)Math.Round((1.3f * h) + height + bounds.Height)),
                Sampler = KnownResamplers.NearestNeighbor
            }));
            
            var r = new Rectangle(bw - 5, bh - 5, w + 10, h + 10);

            bg.Mutate(x => x.Draw(Pens.Solid(Color.White, 3), r));

            var location = new PointF(bw + padding, r.Bottom + (.65f * bh));

            var to = new TextOptions
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                WrapTextWidth = wrap,
                FallbackFonts = 
                {
                    SystemFonts.Find("Twemoji")
                }
            };

            var options = new TextGraphicsOptions(new GraphicsOptions(), to);
            bg.Mutate(x => x.DrawText(options, title, tFont, Color.White, location));
            location.Y += (.65f * bh) + tBounds.Height;
            bg.Mutate(x => x.DrawText(options, text, font, Color.White, location));

            if (source.Frames.Count() > 1)
            {
                var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
                for (int i = 1; i < source.Frames.Count; i++)
                {
                    var frame = bg.Frames.CloneFrame(0).Frames[0];
                    frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                    bg.Frames.InsertFrame(i, frame);
                }
                        
                return GifFilter((Image)bg, source, new Point(bw, bh), source.Size(), 0f, Overlay);
            }
            
            return Overlay((Image)bg, source, new Point(bw, bh), source.Size());
        }

        public async Task<string> VideoFilter(string url, Func<Image, string, Image> func, string arg1)
        {
            if (!await _net.IsVideoAsync(url)) return "";
            var id = await _net.DownloadToFileAsync(url);
            
            var framerate = _reg.Match(await Shell.Run($"ffprobe -hide_banner -show_streams {id}", true)).Value.Replace(" fps", "");

            var tmp = Directory.CreateDirectory($"tmp-{id}");
            await Shell.Run($"ffmpeg -i {id} -hide_banner -vn {tmp.Name}/{id}.aac", true);
            await Shell.Run($"ffmpeg -i {id} -r {framerate} -f image2 -hide_banner {tmp.Name}/frame-%d.png", true);

            var files = tmp.EnumerateFiles().Where(x => x.Name.Contains(".png"));
            for (int i = 0; i < files.Count(); i++)
            {
                var f = files.ElementAt(i);
                var img = Image.Load(f.FullName);

                if (i == 0 && (img.Width > 1000 || img.Height > 1000))
                    throw new Exception("Video too large.");

                img = func(img, arg1);

                File.Delete(f.FullName);
                Save(img, f.FullName);
            }

            await Shell.Run($"ffmpeg -f image2 -framerate {framerate} -i {tmp.Name}/frame-%d.png -i {tmp.Name}/{id}.aac -hide_banner -c:v libx264 -c:a copy {id}.mp4", true);

            File.Delete(id);
            Directory.Delete(tmp.Name, true);

            return $"{id}.mp4";
        }
        public async Task<string> VideoFilter(string url, Func<Image, string, string, Image> func, string arg1, string arg2)
        {
            if (!await _net.IsVideoAsync(url)) return "";
            var id = await _net.DownloadToFileAsync(url);
            
            var framerate = _reg.Match(await Shell.Run($"ffprobe -hide_banner -show_streams {id}", true)).Value.Replace(" fps", "");

            var tmp = Directory.CreateDirectory($"tmp-{id}");
            await Shell.Run($"ffmpeg -i {id} -hide_banner -vn {tmp.Name}/{id}.aac", true);
            await Shell.Run($"ffmpeg -i {id} -r {framerate} -f image2 -hide_banner {tmp.Name}/frame-%04d.png", true);

            foreach (var f in tmp.EnumerateFiles().Where(x => x.Name.Contains(".png")))
            {
                var img = Image.Load(f.FullName);

                if (img.Width > 1000 || img.Height > 1000)
                    throw new VideoException("Video too large.");

                img = func(img, arg1, arg2);

                File.Delete(f.FullName);
                Save(img, f.FullName);
            }

            await Shell.Run($"ffmpeg -f image2 -framerate {framerate} -i {tmp.Name}/frame-%04d.png -i {tmp.Name}/{id}.aac -hide_banner -c:v libx264 -c:a copy {id}.mp4", true);

            File.Delete(id);
            Directory.Delete(tmp.Name, true);

            return $"{id}.mp4";
        }
 
        public Image GifFilter(Image source, Func<Image, Image> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");
            
            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                f = func(f);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }

            return source;
        }
        public Image GifFilter(Image source, float x, Func<Image, float, Image> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");
            
            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                f = func(f, x);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }

            return source;
        }
        public Image GifFilter(Image source, int x, Func<Image, int, Image> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");
            
            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                f = func(f, x);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }

            return source;
        }
        public Image GifFilter(Image source, string x, Func<Image, string, Image> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");

            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                f = func(f, x);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }

            return source;
        }
        public Image GifFilter(Image source, string x, string y, Func<Image, string, string, Image> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");

            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                f = func(f, x, y);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }

            return source;
        }
        public Image GifFilter(Image source, int x, int y, int z, Func<Image, int, int, int, Image> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");

            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                f = func(f, x, y, z);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }

            return source;
        }
        public Image GifFilter(Image source, Image x, Point y, Size z, float w, Func<Image, Image, Point, Size, float, Image> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");
                
            if (x.Frames.Count > 1)
            {
                var delay = x.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
                for (int i = 0; i < source.Frames.Count; i++)
                {
                    var f = source.Frames.CloneFrame(i);
                    var h = x.Frames.CloneFrame(i);
                    f = func(f, h, y, z, w);
                    var frame = f.Frames[0];
                    frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                    source.Frames.RemoveFrame(i);
                    source.Frames.InsertFrame(i, frame);
                }
            }
            else
                for (int i = 0; i < source.Frames.Count; i++)
                {
                    var f = source.Frames.CloneFrame(i);
                    f = func(f, x, y, z, w);
                    source.Frames.RemoveFrame(i);
                    source.Frames.InsertFrame(i, f.Frames[0]);
                }

            return source;
        }
        public Image GifFilterR(Image source, float x, Func<Image, float, Image> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");

            var newSource = func(source.Frames.CloneFrame(0), x);
            
            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = func(source.Frames.CloneFrame(i), x);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                newSource.Frames.InsertFrame(i, frame);
            }

            return newSource;
        }
        public Image ResizeGif(Image source, int x, int y)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");

            var newImg = Image.Load(new byte[] 
            { 
                137, 80, 78, 71, 13, 
                10, 26, 10, 0, 0, 
                0, 13, 73, 72, 68, 
                82, 0, 0, 0, 1, 
                0, 0, 0, 1, 8, 
                2, 0, 0, 0, 144, 
                119, 83, 222, 0, 
                0, 0, 9, 112, 72, 
                89, 115, 0, 0, 14, 
                196, 0, 0, 14, 196, 
                1, 149, 43, 14, 27, 
                0, 0, 0, 12, 73, 68, 
                65, 84, 120, 156, 99, 
                49, 52, 179, 6, 0, 1, 
                78, 0, 167, 244, 36, 
                3, 55, 0, 0, 0, 
                0, 73, 69, 78, 68, 
                174, 66, 96, 130
            }); //1 pixel image

            newImg.Mutate(h => h.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Stretch,
                Size = new SixLabors.ImageSharp.Size(x, y),
                Sampler = KnownResamplers.MitchellNetravali
            }));

            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                f = Resize(f, x, y);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;

                if (i == 0) 
                {
                    newImg.Frames.AddFrame(frame);
                    newImg.Frames.RemoveFrame(0);
                }
                else
                    newImg.Frames.InsertFrame(i, frame);
            }

            return newImg;
        }
        
        public async Task<Image> VideoToGif (string url)
        {
            if (!await _net.IsVideoAsync(url)) throw new InvalidOperationException("Not a video.");

            var src = Image.Load(new byte[]
            {
                137, 80, 78, 71, 13, 
                10, 26, 10, 0, 0, 
                0, 13, 73, 72, 68, 
                82, 0, 0, 0, 1, 
                0, 0, 0, 1, 8, 
                6, 0, 0, 0, 31, 
                21, 196, 137, 0, 0, 
                0, 11, 73, 68, 65, 
                84, 8, 215, 99, 248, 
                15, 4, 0, 9, 251, 
                3, 253, 99, 38, 197, 
                143, 0, 0, 0, 0, 
                73, 69, 78, 68, 174, 
                66, 96, 130
            }); //1px image

            var id = await _net.DownloadToFileAsync(url);
            
            var framerate = _reg.Match(await Shell.Run($"ffprobe -hide_banner -show_streams {id}", true)).Value.Replace(" fps", "");

            var delay = Math.Max((int)Math.Round(100d / int.Parse(framerate)), 1);

            var tmp = Directory.CreateDirectory($"tmp-{id}");
            await Shell.Run($"ffmpeg -i {id} -r {framerate} -f image2 -hide_banner {tmp.Name}/frame-%04d.png", true);

            var h = tmp.EnumerateFiles().Where(x => x.Name.Contains(".png")).ToArray();
            for (int i = 0; i < h.Count(); i++)
            {
                var img = Image.Load(h[i].FullName);
                var frame = img.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                if (i == 0)
                {
                    if (img.Width > 1000 || img.Height > 1000)
                        throw new VideoException("Video too large.");

                    src.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Stretch,
                        Size = new Size(img.Width, img.Height),
                        Sampler = KnownResamplers.NearestNeighbor
                    }));

                    src.Frames.AddFrame(frame);
                    src.Frames.RemoveFrame(0);
                }
                else
                    src.Frames.InsertFrame(i, frame);

                File.Delete(h[i].FullName);
            }
            
            File.Delete(id);
            Directory.Delete(tmp.Name, true);

            return src;
        }

        public async Task<Image> PlaceBelow(string url, string belowUrl, bool resize = true) => PlaceBelow(await DownloadFromUrlAsync(url), await DownloadFromUrlAsync(belowUrl), resize);
        public Image PlaceBelow(Image source, Image below, bool resize = true)
        {
            var src = (Image)Image.Load(new byte[]
            {
                137, 80, 78, 71, 13, 
                10, 26, 10, 0, 0, 
                0, 13, 73, 72, 68, 
                82, 0, 0, 0, 1, 
                0, 0, 0, 1, 8, 
                6, 0, 0, 0, 31, 
                21, 196, 137, 0, 0, 
                0, 11, 73, 68, 65, 
                84, 8, 215, 99, 248, 
                15, 4, 0, 9, 251, 
                3, 253, 99, 38, 197, 
                143, 0, 0, 0, 0, 
                73, 69, 78, 68, 174, 
                66, 96, 130
            }); //1px image

            if (resize)
                below.Mutate(x => x.Resize(source.Width, source.Width));

            src.Mutate(x => x.Resize(source.Width, source.Height + below.Height));

            src = Overlay(src, source, new Point(0, 0), source.Size());
            src = Overlay(src, below, new Point(0, source.Height), below.Size());

            return src;
        }

        public async Task<Image> DrawText(string url, string text, string topText = null) => DrawText(await DownloadFromUrlAsync(url), text, topText);
        public Image DrawText(Image source, string text, string bottomText = null)
        {
            if (source.Frames.Count > 1)
                return GifFilter(source, text, bottomText, DrawText);
            else
            {
                if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(bottomText))
                    throw new ImageException("Text cannot be blank.");
            
                var size = Math.Min(source.Height / 10, source.Width / 10);
                Font f = SystemFonts.CreateFont("Impact", size);

                float padding = 0.05f * source.Width;
                float width = source.Width - (2 * padding);

                var to = new TextOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    WrapTextWidth = width,
                    FallbackFonts = 
                    {
                        SystemFonts.Find("Twemoji")
                    }
                };

                var options = new TextGraphicsOptions(new GraphicsOptions(), to);

                PointF location = new PointF(padding, .95f * source.Height);

                float pSize = Math.Max(size / 10f, 1f);

                if (bottomText == null)
                {
                    source.Mutate(x => x.DrawText(options, text, f, Pens.Solid(Color.Black, pSize), location));
                    source.Mutate(x => x.DrawText(options, text, f, Color.White, location));
                }
                else
                {
                    source.Mutate(x => x.DrawText(options, bottomText, f, Pens.Solid(Color.Black, pSize), location));
                    source.Mutate(x => x.DrawText(options, bottomText, f, Color.White, location));
                    options.TextOptions.VerticalAlignment = VerticalAlignment.Top;
                    location = new PointF(padding, .05f * source.Height);
                    source.Mutate(x => x.DrawText(options, text, f, Pens.Solid(Color.Black, pSize), location));
                    source.Mutate(x => x.DrawText(options, text, f, Color.White, location));
                }
                return source;
            }
        }

        public async Task<Image> Resize(string url, int x, int y) => Resize(await DownloadFromUrlAsync(url), x, y);
        public Image Resize(Image source, int x, int y)
        {
            if (source.Frames.Count > 1)
                source = ResizeGif(source, x, y);
            else
                source.Mutate(h => h.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Stretch,
                    Size = new SixLabors.ImageSharp.Size(x, y),
                    Sampler = KnownResamplers.MitchellNetravali
                }));

            return source;
        }

        public async Task<Image> SpeedUp(string url, double speed) => SpeedUp(await DownloadFromUrlAsync(url), 2);
        public Image SpeedUp(Image source, double speed)
        {
            if (speed > 1000 || speed <= 0) speed = 2;
            
            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var frame = source.Frames.CloneFrame(i).Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = (int)Math.Max(Math.Round(delay / speed), 2);

                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }

            return source;
        }
        

        public async Task<Image> Reverse(string url) => Reverse(await DownloadFromUrlAsync(url));
        public Image Reverse(Image source)
        {
            var ct = source.Frames.Count;
            for (int i = 0; i < ct / 2; i++)
            {
                var newIn = ct - 1 - i;
                var newF = source.Frames.CloneFrame(i).Frames[0];
                var f = source.Frames.CloneFrame(newIn).Frames[0];

                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, f);
                
                source.Frames.RemoveFrame(newIn);
                source.Frames.InsertFrame(newIn, newF);
            }

            return source;
        }

        public string GetNekoEndpoints(bool nsfw)
        {
            if (nsfw)
                return $"NSFW: {string.Join(", ", _nkeps.Nsfw.Keys)}";
            else
                return $"SFW: {string.Join(", ", _nkeps.Sfw.Keys)}"; 
        }
        public async Task<string> GetNekoImageAsync(bool nsfw, ulong gId, string ep = "neko")
        {
            var url = "";
            
            Dictionary<string, string> endpoints;

            if (nsfw)
            {
                endpoints = _nkeps.Nsfw;
                if (!endpoints.ContainsKey(ep)) 
                    ep = endpoints["nekoGif"];
                else
                    ep = endpoints[ep];

            }
            else
            {
                endpoints = _nkeps.Sfw;
                if (!endpoints.ContainsKey(ep)) 
                    ep = endpoints["neko"];
                else
                    ep = endpoints[ep];
            }

            while (true)
            {
                var res = JsonConvert.DeserializeObject<JObject>(await _net.DownloadAsStringAsync($"https://nekos.life/api/v2{ep}"));
                url = res["url"].Value<string>();
                var obj = new GuildImage(url, gId);
                if (!sentImages.ContainsObj(obj))
                {
                    sentImages.Add(obj);
                    break;
                }
            }
            
            return url;
        }

        public async Task<GuildImage> GetBooruImageAsync(ulong gId, string query)
        {
            var img = new GuildImage(null, gId);

            for (int i = 0; i < 10; i++)
            {
                var res = JsonConvert.DeserializeObject<JObject>(await _net.DownloadAsStringAsync($"https://cure.ninja/booru/api/json/{i}?f=e&s=50&o=r&q={WebUtility.UrlEncode(query)}"));               
                if (GetBooruImage(res["results"], gId, query, out img)) break;
            }

            return img;
        }
        private bool GetBooruImage(IEnumerable<JToken> data, ulong gId, string query, out GuildImage image)
        {
            for (int i = 0; i < data.Count(); i++)
            {
                var r = data.ElementAt(i);
                var url = r["url"].Value<string>();
                var un = r["userName"].Value<string>();
                var s = r["sourceURL"].Value<string>();
                s = string.IsNullOrWhiteSpace(s) ? s : "unknown";
                
                image = new GuildImage(url, gId, s, un, title: $"{r["id"].Value<string>()} - {r["source"].Value<string>()}");

                if (!sentImages.ContainsObj(image) && !string.IsNullOrWhiteSpace(un))
                {
                    sentImages.Add(image);
                    return true;
                }
            }
            image = new GuildImage(null, gId);
            return false;
        }

        public async Task<GuildImage> GetRedditImageAsync(ulong gId, string name, bool nsfw, string mode = "top")
        {
            string sub = "";

            var subs = SubredditCollection.Load($"{name}.txt", name);
            sub = subs[_rand.RandomNumber(0, subs.Count - 1)];
            if (!sub.Contains("r/"))
                if (!sub.Contains("u/"))
                    sub = $"r/{sub}";

            return await GetRedditImageAsync(sub, gId, nsfw, mode);
        }
        public async Task<GuildImage> GetRedditImageAsync(string sub, ulong gId, bool nsfw, string mode = "top")
        {
            if (!sub.Contains("r/"))
                if (!sub.Contains("u/"))
                    sub = $"r/{sub}";

            var img = new GuildImage(null, gId, sub: sub);

            var accepted = new List<string>
            {
                "top",
                "best",
                "new",
                "rising",
                "hot",
                "controversial"
            };

            if (!accepted.Contains(mode))
                mode = "top";

            var post = JsonConvert.DeserializeObject<JObject>(await _net.DownloadAsStringAsync($"https://www.reddit.com/{sub}/{mode}.json?limit=50"))["data"];
            var count = post["children"].Count();
            for (int i = 0; i < 10; i++) //scan 10 pages
            {
                var pages = new List<string> 
                { 
                    post["before"].Value<string>(), 
                    post["after"].Value<string>() 
                };
 
                var postdata = post["children"].Shuffle();
                if (GetImage(postdata, gId, nsfw, out img)) break;

                if (postdata.Count() < count) return img; //no more pages
                else
                    post = JsonConvert.DeserializeObject<JObject>(await _net.DownloadAsStringAsync($"https://www.reddit.com/{sub}/{mode}.json?limit=50&page={pages[1]}"))["data"];
            }
            
            img.Subreddit = sub;
            return img;
        }

        private bool GetImage(IEnumerable<JToken> postdata, ulong gId, bool nsfw, out GuildImage image)
        {
            for (int i = 0; i < postdata.Count(); i++)
            {
                var post = postdata.ElementAt(i)["data"];
                var hint = post["post_hint"];
                if (post["url"] != null && hint != null && (hint.Value<string>() == "image" || hint.Value<string>() == "hosted:video"))
                {
                    var title = post["title"].Value<string>();
                    if (title.Length > 256)
                        title = $"{title.Substring(0, 253)}...";

                    string url = post["url"].Value<string>();
                    string type = hint.Value<string>();
                    if (type != "image")
                    {
                        url = post["media"]["reddit_video"]["fallback_url"].Value<string>();
                        type = "video";
                    }

                    image = new GuildImage(url, gId, author: $"u/{post["author"].Value<string>()}", title: title, type: type);

                    if (!sentImages.ContainsObj(image))
                    {
                        if (nsfw)
                        {
                            sentImages.Add(image);
                            return true;
                        }
                        else if (!post["over_18"].Value<bool>())
                        {
                            sentImages.Add(image);
                            return true;
                        } 
                    }
                }
            }
            image = new GuildImage(null, gId);
            return false;
        }

        public async Task<ImageProperties> GetInfo(string url)
        {
            var img = await DownloadFromUrlAsync(url);
            return new ImageProperties(img.Width, img.Height, img.Frames.Count, img.PixelType.BitsPerPixel, 
            Math.Round(100d / img.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay, 3), _format.DefaultMimeType,
            Math.Round(img.Metadata.HorizontalResolution, 3), Math.Round(img.Metadata.VerticalResolution, 3));
        }

        public async Task<string> ParseUrlAsync(string url, SocketUserMessage msg)
        {
            if (url != null) url = url.Trim('<').Trim('>');
            if (url == null || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                if (msg.Attachments.Count == 0)
                {
                    var previousmsg = await _misc.GetPreviousMessageAsync(msg.Channel as SocketTextChannel);
                    if (previousmsg.Attachments.Count > 0)
                        url = previousmsg.Attachments.First().Url;
                    else
                        if (Uri.IsWellFormedUriString(previousmsg.Content, UriKind.Absolute))
                            url = previousmsg.Content;
                        else
                            throw new ImageException("Try the command with a url, or attach an image.");
                }
                else
                    url = msg.Attachments.First().Url;
            }
            return url;
        }

        public string Save(Image source, string path = null)
        {
            var id = _rand.GenerateId();

            if (path != null)
                using (var file = File.Open(path, FileMode.OpenOrCreate))
                    source.SaveAsPng(file);
            else
            {
                if ((_format != null && _format.DefaultMimeType == "image/gif") || source.Frames.Count > 1) 
                {
                    source.Metadata.GetFormatMetadata(GifFormat.Instance).ColorTableMode = GifColorTableMode.Local;
                    source.Metadata.GetFormatMetadata(GifFormat.Instance).RepeatCount = 0;
                    path = $"{id}.gif";
                    using (var file = File.Open(path, FileMode.OpenOrCreate))
                        source.SaveAsGif(file);
                }
                else
                {
                    path = $"{id}.png";
                    using (var file = File.Open(path, FileMode.OpenOrCreate))
                        source.SaveAsPng(file);
                }
            }

            source.Dispose();
            return path;
        }

        public string SaveAsJpeg(Image source, int quality)
        {
            var id = _rand.GenerateId();

            string path;
            path = $"{id}.jpg";

            using (var file = File.Open(path, FileMode.OpenOrCreate))
            {
                JpegEncoder s = new JpegEncoder
                {
                    Quality = quality,
                };
                source.SaveAsJpeg(file, s);
                source.Dispose();
                return path;
            }
        }

        public async Task SendToChannelAsync(Image img, ISocketMessageChannel ch)
        {
            var path = Save(img);
            var ext = path.Split('.')[1];
            var len = new FileInfo(path).Length;
            if (len > 8388119) //allegedly discord's limit
                await ch.SendMessageAsync(await _net.UploadAsync(path, ext));
            else
                await ch.SendFileAsync(path);
            File.Delete(path);
        }
        public async Task SendToChannelAsync(string path, ISocketMessageChannel ch)
        {
            var ext = path.Split('.')[1];
            var len = new FileInfo(path).Length;
            if (len > 8388119) //allegedly discord's limit
                await ch.SendMessageAsync(await _net.UploadAsync(path, ext));
            else
                await ch.SendFileAsync(path);
            File.Delete(path);
        }

        public async Task<Image> DownloadFromUrlAsync(string url) => Image.Load(await _net.DownloadFromUrlAsync(url), out _format);
    }
}