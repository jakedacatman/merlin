using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;
using Discord;

namespace donniebot.commands
{
    [Name("Image")]
    public class OverlayCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public OverlayCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("overlay")]
        [Alias("o")]
        [Summary("Overlays an image on another image.")]
        public async Task OverlayAsync([Summary("The image to overlay.")] string overlayUrl, 
            [Summary("The x-position to overlay to. Defaults to the center of the background image.")] int x = -1, 
            [Summary("The y-position to overlay to. Defaults to the center of the background image.")] int y = -1, 
            [Summary("The width of the overlaid/foreground image.")] int width = 0, 
            [Summary("The height of the foreground image.")] int height = 0, 
            [Summary("The image to be overlaid upon/background image.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            overlayUrl = await _img.ParseUrlAsync(overlayUrl, Context.Message);
            var img = await _img.OverlayAsync(url, overlayUrl, x, y, width, height);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}