using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;

namespace donniebot.commands
{
    [Name("Image")]
    public class ImageInfoCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public ImageInfoCommand(DiscordShardedClient client, ImageService img, MiscService misc, RandomService rand)
        {
            _client = client;
            _img = img;
            _misc = misc;
            _rand = rand;
        }

        [Command("info")]
        [Alias("i", "inf")]
        [Summary("Gets some information about an image.")]
        public async Task ImageInfoAsync([Summary("The image to get the information for.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var info = await _img.GetInfoAsync(url);
            var em = new EmbedBuilder()
                .WithColor(_rand.RandomColor())
                .WithCurrentTimestamp()
                .WithThumbnailUrl(url)
                .WithFields(new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder().WithName("Width").WithValue(info.Width).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Height").WithValue(info.Height).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Resolution").WithValue($"{(ulong)info.Width * (ulong)info.Height} px").WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Frames").WithValue(info.Frames).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Color depth").WithValue(info.BitsPerPixel + " bpp").WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Frames/second").WithValue((double.IsInfinity(info.FPS) ? "unknown " : info.FPS.ToString()) + " fps").WithIsInline(true),
                    new EmbedFieldBuilder().WithName("MIME type").WithValue(info.MimeType).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Resolution").WithValue($"{info.HorizontalResolution} dpi * {info.VerticalResolution} dpi").WithIsInline(true),
                });
            await Context.Channel.SendMessageAsync(embed: em.Build(), messageReference: new MessageReference(Context.Message.Id));
        }
    }
}