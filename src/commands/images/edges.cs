using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;
using Discord;

namespace merlin.commands
{
    [Name("Image")]
    public class EdgesCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public EdgesCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("edges")]
        [Alias("ed")]
        [Summary("Detects an image's edges.")]
        public async Task EdgesAsync([Summary("The image to modify.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var img = await _img.EdgesAsync(url);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}