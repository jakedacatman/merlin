using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Nsfw")]
    public class NekoNsfwCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly ImageService _img;
        private readonly RandomService _rand;

        public NekoNsfwCommand(DiscordShardedClient client, MiscService misc, ImageService img, RandomService rand)
        {
            _client = client;
            _misc = misc;
            _img = img;
            _rand = rand;
        }

        [Command("nekonsfw")]
        [Alias("nen", "nekon", "nekn")]
        [RequireNsfw]
        [Summary("Grabs an NSFW image from the nekos.life API.")]
        public async Task NekoNSFWAsync([Summary("The endpoint to pull from. Run the command with argument `list` for a list of endpoints.")] string ep = "nsfw_neko_gif")
        {
            if (ep == "list")
            {
                await ReplyAsync(_img.GetNekoEndpoints(true));
                return;
            }

            var info = await _img.GetNekoImageAsync(true, Context.Guild.Id, ep);
            await ReplyAsync(embed: (new EmbedBuilder()
                .WithColor(_rand.RandomColor())
                .WithImageUrl(info)
                .WithTimestamp(DateTime.UtcNow)
            ).Build(), messageReference: new MessageReference(Context.Message.Id), allowedMentions: AllowedMentions.None);
        }
    }
}