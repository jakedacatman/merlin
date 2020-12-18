using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Interactivity;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Misc")]
    public class BulbapediaCommand : ModuleBase<ShardedCommandContext>
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
        [Alias("bu", "pokedex")]
        [Summary("Searches Bulbapedia for an article.")]
        public async Task BulbapediaCmd([Summary("The search term."), Remainder] string term)
        {
            try
            {
                var article = await _net.GetBulbapediaArticleAsync(term);
                if (article.Title == "" && article.Url == "")
                    await ReplyAsync("Failed to find the article.");
                else
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithTitle(article.Title)
                        .WithUrl(article.Url)
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
