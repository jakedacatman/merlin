using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;

namespace donniebot.commands
{
    [Name("Nsfw")]
    public class BooruCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly ImageService _img;
        private readonly RandomService _rand;

        public BooruCommand(DiscordShardedClient client, MiscService misc, ImageService img, RandomService rand)
        {
            _client = client;
            _misc = misc;
            _img = img;
            _rand = rand;
        }

        [Command("booru")]
        [Alias("r34", "bo")]
        [Summary("Grabs a random NSFW image from any of several *booru websites.")]
        [RequireNsfw]
        public async Task BooruAsync([Summary("The search query."), Remainder] string query)
        {
            await ReplyAsync("The API this command uses is currently down. Sorry for the inconvenience.");
            return;

            /*
            var img = await _img.GetBooruImageAsync(Context.Guild.Id, query);

            if (img.Url == null)
            {
                await ReplyAsync("There are no more images.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle(img.Title)
                .WithUrl(img.Url)
                .WithImageUrl(img.Url)
                .WithColor(_rand.RandomColor())
                .WithTimestamp(DateTime.UtcNow)
                .WithFooter($"Posted by {img.Author}");
                    
            await ReplyAsync(embed: embed.Build());
            */
        }
    }
}