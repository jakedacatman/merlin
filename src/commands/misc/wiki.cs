using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using merlin.services;

namespace merlin.commands
{
    [Name("Misc")]
    public class WikiCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly NetService _net;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public WikiCommand(NetService net, MiscService misc, RandomService rand)
        {
            _net = net;
            _misc = misc;
            _rand = rand;
        }

        [Command("wiki")]
        [Alias("w", "wikipedia")]
        [Summary("Searches Wikipedia for an article.")]
        public async Task WikiAsync([Summary("The search term."), Remainder] string term)
        {
            var article = await _net.GetWikipediaArticleAsync(term);
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
    }
}