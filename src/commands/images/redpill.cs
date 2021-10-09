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
    public class RedpillCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public RedpillCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("redpill")]
        [Alias("rp")]
        [Summary("Creates an image with two choices..")]
        public async Task RedpillAsync([Summary("The first choice (red pill).")] string choice1, [Summary("The second choice (blue pill).")] string choice2)
        {
            var img = await _img.RedpillAsync(choice1, choice2);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}