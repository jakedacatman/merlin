using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Image")]
    public class NekoNsfwCommand : InteractiveBase<ShardedCommandContext>
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
        public async Task NekoNSFWCmd([Summary("The endpoint to pull from. Do `don.nen list` for a list of endpoints.")] string ep = "nsfw_neko_gif")
        {
            try
            {
                var info = await _img.GetNekoImageAsync(true, Context.Guild.Id, ep);
                if (info.Substring(0, 5) == "NSFW:")
                    await ReplyAsync(info);
                else
                    await ReplyAsync(embed: (new EmbedBuilder()
                        .WithColor(_rand.RandomColor())
                        .WithImageUrl(info)
                        .WithTimestamp(DateTime.UtcNow)
                    ).Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}