using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;

namespace merlin.commands
{
    [Name("Image")]
    public class NekoCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly ImageService _img;
        private readonly RandomService _rand;

        public NekoCommand(DiscordShardedClient client, MiscService misc, ImageService img, RandomService rand)
        {
            _client = client;
            _misc = misc;
            _img = img;
            _rand = rand;
        }

        [Command("neko")]
        [Alias("ne", "nek")]
        [Summary("Grabs an image from the nekos.life API.")]
        public async Task NekoAsync([Summary("The endpoint to pull from. Run the command with argument `list` for a list of endpoints.")] string ep = "neko")
        {
            if (ep == "list")
            {
                await ReplyAsync(_img.GetNekoEndpoints(false));
                return;
            }

            var info = await _img.GetNekoImageAsync(false, Context.Guild.Id, ep);
            await ReplyAsync(embed: (new EmbedBuilder()
                .WithColor(_rand.RandomColor())
                .WithImageUrl(info)
                .WithTimestamp(DateTime.UtcNow)
            ).Build(), messageReference: new MessageReference(Context.Message.Id), allowedMentions: AllowedMentions.None);
        }
    }
}