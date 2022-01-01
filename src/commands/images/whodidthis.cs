using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;
using Discord;
using SixLabors.ImageSharp;

namespace donniebot.commands
{
    [Name("Image")]
    public class WhoDidThisCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public WhoDidThisCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("whodidthis")]
        [Alias("wdd")]
        [Summary("WHO DID THIS 😂")]
        public async Task WDDAsync([Summary("The image in the middle.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var img = await _img.DownloadFromUrlAsync(url);

            var wddUrl = "https://i.jakedacatman.me/TU37X.png";
            var wddOverlay = await _img.DownloadFromUrlAsync(wddUrl);
            
            var size = new SizeF(1000, 512);
            var location = new PointF(0, 244);

            _img.Overlay(wddOverlay, img, location, size);
            await _img.SendToChannelAsync(wddOverlay, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}