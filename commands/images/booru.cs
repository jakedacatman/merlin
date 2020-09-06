using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Image")]
    public class BooruCommand : InteractiveBase<ShardedCommandContext>
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
        [Summary("Grabs a random NSFW image from several *booru websites.")]
        [RequireNsfw]
        public async Task BooruCmd([Summary("The search query.")] string query)
        {
            try
            {
                var info = await _img.GetBooruImageAsync(Context.Guild.Id, query);

                var embed = new EmbedBuilder()
                    .WithTitle(info["source"])
                    .WithImageUrl(info["url"])
                    .WithColor(_rand.RandomColor())
                    .WithTimestamp(DateTime.UtcNow)
                    .WithFooter($"Posted by {info["author"]}");
                        
                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}