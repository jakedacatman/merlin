using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;
using Discord;

namespace merlin.commands
{
    [Name("Image")]
    public class GreyscaleCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public GreyscaleCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("greyscale")]
        [Alias("gr", "grey", "gray", "grayscale")]
        [Summary("Makes an image greyscale.")]
        public async Task GreyscaleAsync([Summary("The image to convert to greyscale.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var img = await _img.GreyscaleAsync(url);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}