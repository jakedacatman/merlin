using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord;

namespace donniebot.commands
{
    [Name("Image")]
    public class JpegCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public JpegCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("jpeg")]
        [Alias("jpg", "j")]
        [Summary("Applies JPEG compression to an image. Note: JPEG files do not support a transparent background, so any transparency is converted to black.")]
        public async Task JpegAsync([Summary("The percent quality to apply to the image.")] int quality = 10, [Summary("The image to compress.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            if (quality < 0 || quality > 100) quality = 10;
            var img = await _img.JpegAsync(url, quality);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}