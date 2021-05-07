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
    public class DemotivationalCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;
        private readonly NetService _net;

        public DemotivationalCommand(DiscordShardedClient client, ImageService img, MiscService misc, NetService net)
        {
            _client = client;
            _img = img;
            _misc = misc;
            _net = net;
        }

        [Command("demotivational")]
        [Alias("de", "dm")]
        [Summary("Creates a demotivational poster from an image and some text.")]
        public async Task DemotivationalAsync([Summary("The text to write.")]string text, [Summary("The text to put above.")]string title, [Summary("The image to create a poster of.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            if (await _net.IsVideoAsync(url))
            {
                var path = await _img.VideoFilterAsync(url, _img.Demotivational, title, text);
                await _img.SendToChannelAsync(path, Context.Channel, new MessageReference(Context.Message.Id));
            }
            else
            {
                var img = await _img.DemotivationalAsync(url, title, text);
                await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
            }
        }
    }
}