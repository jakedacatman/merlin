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
    public class DrawTextCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;
        private readonly NetService _net;

        public DrawTextCommand(DiscordShardedClient client, ImageService img, MiscService misc, NetService net)
        {
            _client = client;
            _img = img;
            _misc = misc;
            _net = net;
        }

        [Command("drawtext")]
        [Alias("d")]
        [Summary("Draws text on an image.")]
        public async Task DrawTextAsync([Summary("The text to draw.")]string text, [Summary("The optional bottom text to draw.")] string bottomText = null, [Summary("The image to modify.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var img = await _img.DrawTextAsync(url, text, bottomText);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
            img.Dispose();
        }
    }
}