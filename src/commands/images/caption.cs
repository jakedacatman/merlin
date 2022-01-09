using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;
using Discord;

namespace merlin.commands
{
    [Name("Image")]
    public class CaptionCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;
        private readonly NetService _net;

        public CaptionCommand(DiscordShardedClient client, ImageService img, MiscService misc, NetService net)
        {
            _client = client;
            _img = img;
            _misc = misc;
            _net = net;
        }

        [Command("caption")]
        [Alias("c", "cap")]
        [Summary("Captions an image.")]
        public async Task CaptionAsync([Summary("The caption to write.")]string caption, [Summary("The image to caption.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var img = await _img.CaptionAsync(url, caption);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}