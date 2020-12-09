using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Image")]
    public class RedpillCommand : InteractiveBase<ShardedCommandContext>
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
        public async Task RedpillCmd([Summary("The first choice (red pill).")] string choice1, [Summary("The second choice (blue pill).")] string choice2)
        {
            try
            {
                var img = await _img.Redpill(choice1, choice2);
                await _img.SendToChannelAsync(img, Context.Channel);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}