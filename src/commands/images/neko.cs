using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;

namespace donniebot.commands
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
        public async Task NekoCmd([Summary("The endpoint to pull from. Do `don.ne list` for a list of endpoints.")] string ep = "neko")
        {
            try
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
                ).Build(), messageReference: new MessageReference(Context.Message.Id));
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}