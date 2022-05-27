using System;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Collections.Generic;
using System.Threading;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using merlin.classes;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using Discord.WebSocket;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UkooLabs.SVGSharpie.ImageSharp;
using ISImage = SixLabors.ImageSharp.Image;
using Discord;
using SixLabors.ImageSharp.Formats.Png;

namespace merlin.services
{
    public class ImageService
    {
        private readonly MiscService _misc;
        private readonly NetService _net;
        private readonly RandomService _rand;
        private readonly DbService _db;
        private readonly DiscordShardedClient _client;

        private IImageFormat _format;
        private List<GuildImage> sentImages = new List<GuildImage>();

        private readonly FontFamily impactFontFamily;
        private readonly FontFamily linuxLFontFamily;
        private readonly FontFamily limerickFontFamily;
        private readonly FontFamily twemojiFontFamily;
        private readonly FontFamily hanaminFontFamily;
        private readonly FontFamily goulongFontFamily;

        private FontFamily[] fallbackFonts;

        public ImageService(MiscService misc, NetService net, RandomService rand, DbService db, DiscordShardedClient client)
        {
            _misc = misc;
            _net = net;
            _rand = rand;
            _db = db;
            _client = client;


            if (!SystemFonts.Collection.TryGet("Impact", out impactFontFamily))
                throw new Exception("Twemoji Mozilla font not found.");

            if (!SystemFonts.Collection.TryGet("HanaminA", out hanaminFontFamily))
                throw new Exception("HanaminA font not found.");

            if (!SystemFonts.Collection.TryGet("Twemoji Mozilla", out twemojiFontFamily))
                throw new Exception("Twemoji Mozilla font not found.");

            if (!SystemFonts.Collection.TryGet("Linux Libertine", out linuxLFontFamily))
                throw new Exception("Linux Libertine font not found.");

            if (!SystemFonts.Collection.TryGet("LimerickCdSerial-Xbold", out limerickFontFamily))
                throw new Exception("LimerickCdSerial Xbold font not found.");

            if (!SystemFonts.Collection.TryGet("Goulong", out goulongFontFamily))
                throw new Exception("Goulong font not found.");

            fallbackFonts = new[] { twemojiFontFamily, hanaminFontFamily };
        }

        public async Task<ISImage> InvertAsync(string url)
        {
            var source = await DownloadFromUrlAsync(url);
            Invert(source);
            return source;
        }
        public void Invert(ISImage source)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, Invert);
            else
                source.Mutate(x => x.Invert());
        }

        public async Task<ISImage> BrightnessAsync(string url, float brightness)
        {
            var source = await DownloadFromUrlAsync(url);
            Brightness(source, brightness);
            return source;
        }
        public void Brightness(ISImage source, float brightness)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, brightness, Brightness);
            else
                source.Mutate(x => x.Brightness(brightness));
        }

        public async Task<ISImage> BlurAsync(string url, float amount)
        {
            var source = await DownloadFromUrlAsync(url);
            Blur(source, amount);
            return source;
        }
        public void Blur(ISImage source, float amount)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, amount, Blur);
            else
                source.Mutate(x => x.GaussianBlur(amount));
        }

        public async Task<ISImage> GreyscaleAsync(string url)
        {
            var source = await DownloadFromUrlAsync(url);
            Greyscale(source);
            return source;
        }
        public void Greyscale(ISImage source)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, Greyscale);
            else
                source.Mutate(x => x.Grayscale());
        }

        public async Task<ISImage> EdgesAsync(string url)
        {
            var source = await DownloadFromUrlAsync(url);
            Edges(source);
            return source;
        }
        public void Edges(ISImage source)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, Edges);
            else
                source.Mutate(x => x.DetectEdges());
        }

        public async Task<ISImage> ContrastAsync(string url, float amount)
        {
            var source = await DownloadFromUrlAsync(url);
            Contrast(source, amount);
            return source;
        }
        public void Contrast(ISImage source, float amount)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, amount, Contrast);
            else
                source.Mutate(x => x.Contrast(amount));
        }

        public async Task<ISImage> SharpenAsync(string url, float amount)
        {
            var source = await DownloadFromUrlAsync(url);
            Sharpen(source, amount);
            return source;
        }
        public void Sharpen(ISImage source, float amount)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, amount, Sharpen);
            else
                source.Mutate(x => x.GaussianSharpen(amount));
        }

        public async Task<ISImage> PixelateAsync(string url, int size)
        {
            var source = await DownloadFromUrlAsync(url);
            var max = Math.Min(source.Width, source.Height);
            if (size > max) size = max;
            Pixelate(source, size);
            return source;
        }
        public void Pixelate(ISImage source, int size)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, size, Pixelate);
            else
                source.Mutate(x => x.Pixelate(size));
        }
        
        public async Task<ISImage> HueAsync(string url, float amount)
        {
            var source = await DownloadFromUrlAsync(url);
            Hue(source, amount);
            return source;
        }
        public void Hue(ISImage source, float amount)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, amount, Hue);
            else
                source.Mutate(x => x.Hue(amount));
        }

        public async Task<ISImage> BackgroundColorAsync(string url, int r, int g, int b)
        {
            var source = await DownloadFromUrlAsync(url);
            BackgroundColor(source, r, g, b);
            return source;
        }
        public void BackgroundColor(ISImage source, int r, int g, int b)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, r, g, b, BackgroundColor);
            else
                source.Mutate(x => x.BackgroundColor(new SixLabors.ImageSharp.Color(new Rgba64((ushort)r, (ushort)g, (ushort)b, 255))));
        }

        public async Task<ISImage> RotateAsync(string url, float r)
        {
            var source = await DownloadFromUrlAsync(url);
            Rotate(source, r);
            return source;
        }
        public void Rotate(ISImage source, float r)
        {
            if (source.Frames.Count > 1)
                RotateGif(source, r, Rotate);
            else
                source.Mutate(x => x.Rotate(r));
        }

        public async Task<ISImage> CaptionAsync(string url, string caption)
        {
            var source = await DownloadFromUrlAsync(url);
            return Caption(source, caption);
        }
        public ISImage Caption(ISImage source, string caption)
        {
            var font = new Font(limerickFontFamily, source.Width / 12f, FontStyle.Bold);

            float padding = 0.05f * source.Width;
            float wrap = source.Width - (2 * padding);

            var bounds = TextMeasurer.Measure(caption, new SixLabors.Fonts.TextOptions(font) 
            { 
                TextAlignment = TextAlignment.Center,
                WrappingLength = wrap, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center 
            });

            var height = Math.Max((int)(bounds.Height * 1.25), (int)(source.Height / 4.5f));

            var img = new Image<Rgba32>(source.Width, source.Height + height);

            PointF location = new PointF(.5f * source.Width, height * .5f);

            var to = new TextOptions(font)
            {
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                WrappingLength = wrap,
                FallbackFontFamilies = fallbackFonts,
                Origin = location
            };

            img.Mutate(x =>
            {
                x.Fill(SixLabors.ImageSharp.Color.White);
                x.DrawText(to, caption, SixLabors.ImageSharp.Color.Black);
            });

            if (source.Frames.Count() > 1)
            {
                var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
                for (int i = 1; i < source.Frames.Count; i++)
                {
                    var frame = img.Frames.CloneFrame(0).Frames[0];
                    frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                    img.Frames.InsertFrame(i, frame);
                }
                        
                GifFilter((ISImage)img, source, new Point(0, height), source.Size(), 0f, Overlay);
            }
            else 
                Overlay((ISImage)img, source, new Point(0, height), source.Size());

            return img;
        }

        public async Task<ISImage> OverlayAsync(string sourceUrl, string overlayUrl, int x, int y, int width, int height, float rot = 0f)
        {
            var source = await DownloadFromUrlAsync(sourceUrl);
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

            Overlay(source, overlay, location, size, rot);

            return source;
        }
        public void Overlay(ISImage source, ISImage overlay, PointF location, SizeF? size = null, float rot = 0f)
        {
            if (overlay.Frames.Count > 1)
            {
                var delay = overlay.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
                for (int i = 1; i < overlay.Frames.Count; i++)
                {
                    var frame = source.Frames.CloneFrame(0).Frames[0];
                    frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                    source.Frames.InsertFrame(i, frame);
                }
                        
                GifFilter((ISImage)source, overlay, location, size, rot, Overlay);
            }
            else
            {
                if (size is not null && size != overlay.Size())
                    overlay.Mutate(h => h.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Stretch,
                        Size = (Size)size,
                        Sampler = KnownResamplers.MitchellNetravali
                    }));

                if (rot is not 0f)
                {
                    var ow = overlay.Width;
                    var oh = overlay.Height;
                    overlay.Mutate(x => x.Rotate(rot, KnownResamplers.MitchellNetravali));
                    var nw = overlay.Width;
                    var nh = overlay.Height;
            
                    location = new PointF(location.X - ((nw - ow) / 2), location.Y - ((nh - oh) / 2));
                }

                source.Mutate(x => x.DrawImage(overlay, (Point)location, 1f));
            }
        }

        public async Task<ISImage> SaturateAsync(string url, float amount)
        {
            var source = await DownloadFromUrlAsync(url);
            Saturate(source, amount);
            return source;
        }
        public void Saturate(ISImage source, float amount)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, amount, Saturate);
            else
                source.Mutate(x => x.Saturate(amount));
        }

        public async Task<ISImage> GlowAsync(string url)
        {
            var source = await DownloadFromUrlAsync(url);
            Glow(source);
            return source;
        }
        public void Glow(ISImage source)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, Glow);
            else
                source.Mutate(x => x.Glow());
        }

        public async Task<ISImage> PolaroidAsync(string url)
        {
            var source = await DownloadFromUrlAsync(url);
            Polaroid(source);
            return source;
        }
        public void Polaroid(ISImage source)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, Polaroid);
            else
                source.Mutate(x => x.Polaroid());
        }

        public async Task<ISImage> JpegAsync(string url, int quality)
        {
            var source = await DownloadFromUrlAsync(url);
            return Jpeg(source, quality);
        }
        public ISImage Jpeg(ISImage source, int quality)
        {
            if (source.Frames.Count > 1)
                return GifFilter(source, quality, Jpeg);
            else
            {
                var path = SaveAsJpeg(source, quality);
                var f = File.Open(path, FileMode.Open);
                var img = ISImage.Load(f);
                f.Dispose();
                File.Delete(path);
                return img;
            }
        }

        public async Task<ISImage> DemotivationalAsync(string url, string title, string body) => Demotivational(await DownloadFromUrlAsync(url), title, body);
        public ISImage Demotivational(ISImage source, string title, string body)
        {
            var w = source.Width;
            var h = source.Height;

            var tFont = new Font(linuxLFontFamily, w / 6f, FontStyle.Regular);

            float padding = 0.05f * w;
            float wrap = w - (2 * padding);

            var tBounds = TextMeasurer.Measure(title, new SixLabors.Fonts.TextOptions(tFont) 
            {
                TextAlignment = TextAlignment.Center,
                WrappingLength = wrap,
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center
            });

            if (tBounds.Width > wrap) // will the title fit on one line? if not, scale it down
            {
                var ratio = wrap / tBounds.Width;
                var size = tFont.Size * ratio;

                tFont = new Font(linuxLFontFamily, size, FontStyle.Regular);;

                tBounds = TextMeasurer.MeasureBounds(title, new SixLabors.Fonts.TextOptions(tFont) 
                { 
                    TextAlignment = TextAlignment.Center,
                    WrappingLength = wrap, 
                    HorizontalAlignment = HorizontalAlignment.Center, 
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            var font = new Font(goulongFontFamily, tFont.Size / 3f, FontStyle.Regular);

            var bounds = TextMeasurer.MeasureBounds(body, new SixLabors.Fonts.TextOptions(font) 
            { 
                TextAlignment = TextAlignment.Center,
                WrappingLength = wrap, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                VerticalAlignment = VerticalAlignment.Center
            });

            var bw = (int)Math.Round(w / 8d);
            var bh = (int)Math.Round(h / 8d);

            var height = tBounds.Height + bh;

            ISImage bg = new Image<Rgba32>((int)Math.Round((5d / 4d) * w), (int)Math.Round((1.25f * h) + tBounds.Height + bounds.Height));
            bg.Mutate(x => x.Fill(SixLabors.ImageSharp.Color.Black));

            var rWidth = Math.Max(0.05f * bw, 3f);
            var offset = rWidth + 2;
            
            var r = new RectangleF(bw - offset, bh - offset, w + (2 * offset), h + (2 * offset));

            bg.Mutate(x => x.Draw(Pens.Solid(SixLabors.ImageSharp.Color.White, rWidth), r));

            var location = new PointF(0.5f * bg.Width, r.Bottom + bh);

            var titleOptions = new TextOptions(tFont)
            {
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                WrappingLength = wrap,
                FallbackFontFamilies = fallbackFonts,
                Origin = location
            };

            bg.Mutate(x => x.DrawText(titleOptions, title, SixLabors.ImageSharp.Color.White));

            var nextY = tBounds.Height / 2f;// + bh;
                
            location.Y += (nextY);

            var bodyOptions = new TextOptions(font)
            {
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                WrappingLength = wrap,
                FallbackFontFamilies = fallbackFonts,
                Origin = location
            };

            bg.Mutate(x => x.DrawText(bodyOptions, body, SixLabors.ImageSharp.Color.White));
            
            Overlay((ISImage)bg, source, new Point(bw, bh), source.Size());

            return bg;
        }
 
        public void GifFilter(ISImage source, Action<ISImage> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");
            
            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                func(f);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }
        }
        public void GifFilter(ISImage source, float x, Action<ISImage, float> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");
            
            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                func(f, x);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }
        }
        public void GifFilter(ISImage source, int x, Action<ISImage, int> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");
            
            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                func(f, x);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }
        }
        public ISImage GifFilter(ISImage source, int x, Func<ISImage, int, ISImage> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");
            
            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }

            return source;
        }
        public void GifFilter(ISImage source, string x, Action<ISImage, string> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");

            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                func(f, x);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }
        }
        public void GifFilter(ISImage source, string x, string y, Action<ISImage, string, string> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");

            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                func(f, x, y);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }
        }
        public void GifFilter(ISImage source, int x, int y, int z, Action<ISImage, int, int, int> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");

            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                func(f, x, y, z);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                source.Frames.RemoveFrame(i);
                source.Frames.InsertFrame(i, frame);
            }
        }
        public void GifFilter(ISImage source, ISImage x, PointF y, SizeF? z, float w, Action<ISImage, ISImage, PointF, SizeF?, float> func)
        {
            if (x.Frames.Count <= 1 && source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");
                
            if (x.Frames.Count > 1)
            {
                var delay = x.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
                for (int i = 0; i < source.Frames.Count; i++)
                {
                    var f = source.Frames.CloneFrame(i);
                    var h = x.Frames.CloneFrame(i);
                    func(f, h, y, z, w);
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
                    func(f, x, y, z, w);
                    source.Frames.RemoveFrame(i);
                    source.Frames.InsertFrame(i, f.Frames[0]);
                }
        }
        public ISImage RotateGif(ISImage source, float x, Action<ISImage, float> func)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");

            var newSource = source.Frames.CloneFrame(0);
            func(newSource, x);
            
            var delay = source.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay;
            for (int i = 0; i < source.Frames.Count; i++)
            {
                var f = source.Frames.CloneFrame(i);
                func(f, x);
                var frame = f.Frames[0];
                frame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay = delay;
                newSource.Frames.InsertFrame(i, frame);
            }

            return newSource;
        }
        public ISImage ResizeGif(ISImage source, int x, int y)
        {
            if (source.Frames.Count <= 1) throw new InvalidOperationException("can't use a gif filter on a stationary image");

            var newImg = new Image<Rgba32>(x, y);

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
        
        public async Task<string> VideoToGifAsync(string url)
        {
            if (!await _net.IsVideoAsync(url)) throw new VideoException("Not a video.");

            var id = await _net.DownloadToFileAsync(url);
            var tmp = $"{id}.gif";
        
            Regex reg = new Regex(@"[0-9]+(\.[0-9]{1,2})? fps");

            var framerate = reg.Match(await Shell.RunAsync($"ffprobe -hide_banner -show_streams {id}", true)).Value.Replace(" fps", "");    

            Console.WriteLine(await Shell.FfmpegAsync($"-i {id} -r {framerate} -vf \"split[s0][s1];[s0]palettegen=stats_mode=diff[p];[s1][p]paletteuse\" -loop 0 {tmp}", true));


            File.Delete(id);

            return tmp;
        }

        public async Task<ISImage> PlaceBelowAsync(string url, string belowUrl, bool resize = true) => PlaceBelow(await DownloadFromUrlAsync(url), await DownloadFromUrlAsync(belowUrl), resize);
        public ISImage PlaceBelow(ISImage source, ISImage below, bool resize = true)
        {
            var src = new Image<Rgba32>(source.Width, source.Height + below.Height);

            if (resize)
                below.Mutate(x => x.Resize(source.Width, source.Width));

            src.Mutate(x => x.Resize(source.Width, source.Height + below.Height));

            Overlay((ISImage)src, source, new Point(0, 0), source.Size());
            Overlay((ISImage)src, below, new Point(0, source.Height), below.Size());

            return src;
        }

        public async Task<ISImage> DrawTextAsync(string url, string topText, string bottomText = null)
        {
            var source = await DownloadFromUrlAsync(url);
            DrawText(source, topText, bottomText);
            return source;
        }
        public void DrawText(ISImage source, string topText, string bottomText = null)
        {
            if (source.Frames.Count > 1)
                GifFilter(source, topText, bottomText, DrawText);
            else
            {
                if (string.IsNullOrEmpty(topText) && string.IsNullOrEmpty(bottomText))
                    throw new ImageException("Text cannot be blank.");

                float padding = 0.05f * source.Width;
                float width = source.Width -  (2 * padding);

                var maxArea = new SizeF(width, 0.4f * source.Height);
            
                var tSize = Math.Min(source.Height / 10f, source.Width / 10f);
                Font tF = new Font(impactFontFamily, tSize);

                var meta = source.Metadata;

                var tOptions = new SixLabors.Fonts.TextOptions(tF)
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrappingLength = width,
                    TextAlignment = TextAlignment.Center
                };

                var tBounds = TextMeasurer.Measure(topText, tOptions);

                if (tBounds.Height > maxArea.Height || tBounds.Width > maxArea.Width)
                {
                    tSize = Math.Min(maxArea.Height / tBounds.Height * tSize, maxArea.Width / tBounds.Width * tSize);
                    tF = new Font(impactFontFamily, tSize);
                }
            
                var bSize = Math.Min(source.Height / 10f, source.Width / 10f);
                Font bF = new Font(impactFontFamily, bSize);

                var bOptions = new SixLabors.Fonts.TextOptions(bF)
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    WrappingLength = width,
                    TextAlignment = TextAlignment.Center
                };

                var bBounds = TextMeasurer.Measure(bottomText, bOptions);

                if (bBounds.Height > maxArea.Height || bBounds.Width > maxArea.Width)
                {
                    bSize = Math.Min(maxArea.Height / bBounds.Height * bSize, maxArea.Width / bBounds.Width * bSize);
                    bF = new Font(impactFontFamily, bSize);
                }

                PointF location = new PointF(.5f * source.Width, .9f * source.Height);

                var bottomOptions = new TextOptions(bF)
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    WrappingLength = width,
                    FallbackFontFamilies = fallbackFonts,
                    Origin = location
                };

                float tPSize = Math.Max(tSize / 10f, 1f);
                float bPSize = Math.Max(bSize / 10f, 1f);

                if (bottomText == null)
                {
                    source.Mutate(x =>
                    {
                        x.DrawText(bottomOptions, topText, Pens.Solid(SixLabors.ImageSharp.Color.Black, bPSize));
                        x.DrawText(bottomOptions, topText, SixLabors.ImageSharp.Color.White);
                    });
                }
                else
                {
                    source.Mutate(x => 
                    {
                        x.DrawText(bottomOptions, bottomText, Pens.Solid(SixLabors.ImageSharp.Color.Black, bPSize));
                        x.DrawText(bottomOptions, bottomText, SixLabors.ImageSharp.Color.White);
                    });

                    location.Y = .1f * source.Height;

                    var topOptions = new TextOptions(tF)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        WrappingLength = width,
                        FallbackFontFamilies = fallbackFonts,
                        Origin = location
                    };

                    source.Mutate(x => 
                    {
                        x.DrawText(topOptions, topText, Pens.Solid(SixLabors.ImageSharp.Color.Black, tPSize));
                        x.DrawText(topOptions, topText, SixLabors.ImageSharp.Color.White);
                    });
                }
            }
        }

        public async Task<ISImage> ResizeAsync(string url, int x, int y) => Resize(await DownloadFromUrlAsync(url, x, y), x, y);
        public ISImage Resize(ISImage source, int x, int y)
        {
            if (x > 2000 || y > 2000 || x < 1 || y < 1) throw new ImageException("The dimensions were either too small or too large. The maximum is 2000 x 2000 pixels and the minimum is 1 x 1 pixels.");

            if (source.Frames.Count > 1)
                source = ResizeGif(source, x, y);
            else
            {
                if (x != source.Width && y != source.Height)
                    source.Mutate(h => h.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Stretch,
                        Size = new SixLabors.ImageSharp.Size(x, y),
                        Sampler = KnownResamplers.MitchellNetravali
                    }));
            }

            return source;
        }
        public async Task<ISImage> ResizeAsync(string url, float scaleX, float scaleY) => Resize(await DownloadFromUrlAsync(url), scaleX, scaleY);
        public ISImage Resize(ISImage source, float scaleX, float scaleY)
        {
            var x = (int)Math.Round(source.Width * scaleX);
            var y = (int)Math.Round(source.Height * scaleY);

            if (x > 2000 || y > 2000 || x < 0 || y < 0) throw new ImageException("The dimensions were either too small or too large.");

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

        public async Task<ISImage> SpeedUpAsync(string url, double speed) => SpeedUp(await DownloadFromUrlAsync(url), speed);
        public ISImage SpeedUp(ISImage source, double speed)
        {
            if (speed > 1000d || speed <= 0d) speed = 2d;
            
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
        

        public async Task<ISImage> ReverseAsync(string url) => Reverse(await DownloadFromUrlAsync(url));
        public ISImage Reverse(ISImage source)
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

        public async Task<ISImage> BoomerangAsync(string url) => Boomerang(await DownloadFromUrlAsync(url));
        public ISImage Boomerang(ISImage source)
        {
            var newSrc = source.CloneAs<Rgba32>();
            Reverse(newSrc);
            for (int i = 0; i < newSrc.Frames.Count; i++)
                source.Frames.AddFrame(newSrc.Frames[i]);

            return source;
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
                if (GetImage(postdata, gId, nsfw, out img, false)) break;

                if (postdata.Count() < count) return img; //no more pages
                else
                    post = JsonConvert.DeserializeObject<JObject>(await _net.DownloadAsStringAsync($"https://www.reddit.com/{sub}/{mode}.json?limit=50&page={pages[1]}"))["data"];
            }
            
            img.Subreddit = sub;
            return img;
        }

        private bool GetImage(IEnumerable<JToken> postdata, ulong gId, bool nsfw, out GuildImage image, bool doRepeats, bool video = false)
        {
            for (int i = 0; i < postdata.Count(); i++)
            {
                var post = postdata.ElementAt(i)["data"];
                var hint = post["post_hint"]?.Value<string>();
                if (post["url"] != null && (hint == "image" || (hint == "hosted:video" && video)))
                {
                    var title = post["title"].Value<string>();
                    if (title.Length > 256)
                        title = $"{title.Substring(0, 253)}...";

                    string url = post["url"].Value<string>();

                    if (hint == "hosted:video")
                    {
                        url = post["media"]["reddit_video"]["fallback_url"].Value<string>();
                        hint = "video";
                    }

                    if (hint == "image" && url.Substring(url.Length - 4, 4) == ".gif")
                        hint = "gif";

                    image = new GuildImage(url, gId, author: $"u/{post["author"].Value<string>()}", title: title, type: hint);

                        if (!sentImages.ContainsObj(image) || doRepeats)
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

        private readonly SemaphoreSlim dlSem = new SemaphoreSlim(1, 1); //my bot runs on a raspberry pi so i don't want 10 different videos downloading at once
        public async Task<bool> DownloadRedditVideoAsync(string postUrl, SocketGuildChannel channel, bool nsfw = false, Discord.MessageReference msg = null)
        {
            await dlSem.WaitAsync();
            try
            {
                var post = JsonConvert.DeserializeObject<JArray>(await _net.DownloadAsStringAsync($"{postUrl}.json"))[0];
                if (!GetImage(post["data"]["children"], channel.Guild.Id, nsfw, out var img, true, true)) return false;
                if (img.Type != "video" && img.Type != "gif") return false;

                if (img.Type == "gif")
                {
                    var f = $"{_rand.GenerateId()}.gif";
                    var data = await _net.DownloadFromUrlAsync(img.Url);
                    await File.WriteAllBytesAsync(f, data);
                    await SendToChannelAsync(f, channel as ISocketMessageChannel, msg);
                    return true;
                }

                var reg = new Regex("DASH_[0-9]{1,4}");
                var videoUrl = img.Url;
                var audioUrl = reg.Replace(img.Url, "DASH_audio");
                var fn = $"{_rand.GenerateId()}.mp4";

                if (await _net.IsSuccessAsync(audioUrl))
                    await Shell.FfmpegAsync($"-i \"{videoUrl}\" -i \"{audioUrl}\" {fn}");
                else
                {
                    var data = await _net.DownloadFromUrlAsync(videoUrl);
                    await File.WriteAllBytesAsync(fn, data);
                }

                await SendToChannelAsync(fn, channel as ISocketMessageChannel, msg, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                dlSem.Release();
            }
        }

        public async Task<ImageProperties> GetInfoAsync(string url)
        {
            var img = await DownloadFromUrlAsync(url);
            return new ImageProperties(img.Width, img.Height, img.Frames.Count, img.PixelType.BitsPerPixel, 
            Math.Round(100d / img.Frames.RootFrame.Metadata.GetFormatMetadata(GifFormat.Instance).FrameDelay, 3), _format?.DefaultMimeType,
            Math.Round(img.Metadata.HorizontalResolution, 3), Math.Round(img.Metadata.VerticalResolution, 3));
        }

        public async Task<string> ParseUrlAsync(string url, SocketUserMessage msg, bool isNext = false, bool isPiped = false)
        {
            if (url is not null) 
            {
                var reg = new Regex(@"http[s]?:\/\/");

                if (Discord.MentionUtils.TryParseUser(url, out var uId))
                    return _client.GetUser(uId).GetAvatarUrl(size: 512);
                else if (Discord.Emote.TryParse(url, out var e) && await _net.IsSuccessAsync(e.Url))
                    return e.Url;
                else if (url.Length > 7 && url.Substring(0, 7) == "roblox:")
                {
                    var usernameOrId = url.Substring(7);
                    var size = 512;

                    if (usernameOrId.Contains("@"))
                    {
                        var userAndSize = usernameOrId.Split('@');

                        if (!int.TryParse(userAndSize[1], out size))
                            throw new ImageException($"{userAndSize[1]} is not a valid size.");

                        if (size < 100)
                            throw new ImageException($"{size} is not a valid size. (must be 100 or greater)");

                        if (size > 512)
                            throw new ImageException($"{size} is not a valid size. (must be 512 or lower)");

                        usernameOrId = userAndSize[0];
                    }

                    var robloxUrl = $"https://www.roblox.com/avatar-thumbnail/image?width={size}&height={size}&format=png&userId=";
                    if (ulong.TryParse(usernameOrId, out var id))
                    {
                        var res = JsonConvert.DeserializeObject<JObject>(await _net.DownloadAsStringAsync($"https://users.roblox.com/v1/users/{id}"));
                        if (res["errors"] is not null)
                            throw new ImageException($"Invalid Roblox user ID. ({res["errors"][0]["message"].Value<string>()})");
                        else
                            robloxUrl += res["name"].Value<string>();
                    }
                    else
                    {
                        var res = JsonConvert.DeserializeObject<JObject>(await _net.DownloadAsStringAsync($"https://users.roblox.com/v1/users/search?keyword={usernameOrId}&limit=10"));

                        if (!res["data"].Any() || !res["data"].Any(x => x["name"].Value<string>() == usernameOrId))
                            throw new ImageException("Invalid Roblox username.");
                        else
                            robloxUrl += usernameOrId;
                    }

                    return robloxUrl;
                }
                else if (url.Length > 4 && url.Substring(0, 4) == "tag:")
                {
                    var key = url.Substring(4);
                    var tag = _db.GetTag(key, (msg.Channel as SocketGuildChannel).Guild.Id);

                    if (tag is null)
                        throw new ImageException("That tag does not exist.");
                    else
                    {
                        var value = tag.Value;

                        if (!reg.Match(value).Success || !await _net.IsSuccessAsync(value)) 
                            return await ParseUrlAsync(value, msg, true);
                        else 
                            return value; 
                    }
                }
                else
                {
                    var points = url.EnumerateRunes()
                        .Select(x => x.Value.ToString("x4"))
                        .ToList();
                    var svgUrl = $"https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/{string.Join("-", points)}.svg";
                    if (await _net.IsSuccessAsync(svgUrl))
                        return svgUrl;
                    else
                    {
                        points.RemoveAll(x => x == "fe0f");
                        svgUrl = $"https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/{string.Join("-", points)}.svg";
                        
                        if (await _net.IsSuccessAsync(svgUrl))
                            return svgUrl;
                    }

                    url = url.Trim('<').Trim('>');

                    if (!string.IsNullOrWhiteSpace(url) && reg.Match(url).Success && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                        return url;
                    else if (msg is not null && msg.Attachments.Any())
                        return msg?.Attachments.First().Url;
                    else if (isNext)
                        throw new ImageException("Try the command with a URL, or attach an image.");
                    else
                    {
                        var previousmsg = await _misc.GetPreviousMessageAsync(msg.Channel as SocketTextChannel);
                        return await ParseUrlAsync(previousmsg.Content, previousmsg as SocketUserMessage, true); //we don't want it iterating through every message
                    }
                }
            }
            else
            {
                if (msg.Attachments.Any())
                    return msg?.Attachments.First().Url;

                var refMsg = msg?.ReferencedMessage;

                if (refMsg is not null)
                    return await ParseUrlAsync(refMsg.Content, (SocketUserMessage)refMsg, true);

                var previousmsg = await _misc.GetPreviousMessageAsync(msg.Channel as SocketTextChannel);
                return await ParseUrlAsync(previousmsg.Content, previousmsg as SocketUserMessage, true); //we don't want it iterating through every message
            }
        }

        public string Save(ISImage source, string path = null)
        {
            if (path != null)
                using (var file = File.Open(path, FileMode.OpenOrCreate))
                    source.SaveAsPng(file);
            else
            {
                var id = _rand.GenerateId();

                if ((_format?.DefaultMimeType == "image/gif") || source.Frames.Count > 1) 
                {
                    source.Metadata.GetFormatMetadata(GifFormat.Instance).RepeatCount = 0;
                    var e = new GifEncoder()
                    {
                        ColorTableMode = GifColorTableMode.Local
                    };

                    path = $"{id}.gif";
                    using (var file = File.Open(path, FileMode.OpenOrCreate))
                        source.SaveAsGif(file, e);
                }
                else
                {
                    path = $"{id}.png";
                    using (var file = File.Open(path, FileMode.OpenOrCreate))
                        source.SaveAsPng(file, new PngEncoder
                        {
                            ColorType = PngColorType.RgbWithAlpha,
                            TransparentColorMode = PngTransparentColorMode.Preserve
                        });
                }
            }

            source.Dispose();
            return path;
        }

        public string SaveAsJpeg(ISImage source, int quality)
        {
            var id = _rand.GenerateId();

            string path;
            path = $"{id}.jpg";

            using (var file = File.Open(path, FileMode.OpenOrCreate))
            {
                JpegEncoder s = new JpegEncoder { Quality = quality };
                
                source.SaveAsJpeg(file, s);
                source.Dispose();
                return path;
            }
        }

        public async Task SendToChannelAsync(ISImage img, ISocketMessageChannel ch, Discord.MessageReference msg = null, bool ping = false) => await SendToChannelAsync(Save(img), ch, msg, ping);
        public async Task SendToChannelAsync(string path, ISocketMessageChannel ch, Discord.MessageReference msg = null, bool ping = false)
        {
            try
            {
                var ext = path.Split('.')[1];
                var len = new FileInfo(path).Length;

                var doPing = ping ? Discord.AllowedMentions.All : Discord.AllowedMentions.None;

                if (len > 8388119) //allegedly discord's limit
                    await ch.SendMessageAsync(await _net.UploadAsync(path, ext), messageReference: msg, allowedMentions: doPing);
                else
                    await ch.SendFileAsync(path, messageReference: msg, allowedMentions: Discord.AllowedMentions.None);
            }
            finally 
            { 
                File.Delete(path);
            }
        }

        public async Task<ISImage> DownloadFromUrlAsync(string url, int sizeX = 512, int sizeY = 512)
        {
            if (!url.Contains("svg") && (await _net.GetContentTypeAsync(url))?.ToLower() != "image/svg+xml")
            {
                var imgAndFormat = await ISImage.LoadWithFormatAsync(await _net.GetStreamAsync(url));
                _format = imgAndFormat.Format;
                return imgAndFormat.Image;
            }
            else 
            {
                var img = SvgImageRenderer.RenderFromString<Rgba32>(await _net.DownloadAsStringAsync(url), sizeX, sizeY);
                _format = new SvgFormat();
                return img;
            }
        }
    }
}