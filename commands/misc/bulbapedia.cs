using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.Interactive;
using System.Diagnostics;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Misc")]
    public class BulbapediaCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly NetService _net;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public BulbapediaCommand(NetService net, MiscService misc, RandomService rand)
        {
            _net = net;
            _misc = misc;
            _rand = rand;
        }

        [Command("bulbapedia")]
        [Alias("bu", "pokedex", "p")]
        [Summary("Searches Bulbapedia for an article.")]
        public async Task BulbapediaCmd([Summary("The search term."), Remainder] string term)
        {
            try
            {
                var data = await _net.GetBulbapediaArticleAsync(term);
                if (data.Item1 == "" && data.Item2 == "")
                    await ReplyAsync("Failed to find the article.");
                else
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithTitle(data.Item1)
                        .WithUrl(data.Item2)
                        .WithDescription("Click the title to view the article!")
                        .WithColor(_rand.RandomColor())
                    .Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}