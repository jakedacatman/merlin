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
    public class TrollCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public TrollCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("troll")]
        [Alias("tr", "tf")]
        [Summary("Turns an image into a trollface.")]
        public async Task TrollAsync([Summary("The image to turn into a trollface.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var img = await _img.DownloadFromUrlAsync(url);

            var trollUrl = "https://i.jakedacatman.me/I1_fm.png";
            var trollOverlay = await _img.DownloadFromUrlAsync(trollUrl);
            
            var size = new SizeF(0.6f * img.Width, 0.6f * img.Height);
            var location = new PointF((img.Width / 2f) - (size.Width / 2f), (img.Height / 2f) - (size.Height / 2f));

            img = _img.Overlay(img, trollOverlay, location, size);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}