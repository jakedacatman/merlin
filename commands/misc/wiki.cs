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
    public class WikiCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly NetService _net;
        private readonly MiscService _misc;

        public WikiCommand(NetService net, MiscService misc)
        {
            _net = net;
            _misc = misc;
        }

        [Command("wiki")]
        [Alias("w", "wikipedia")]
        [Summary("Searches Wikipedia for an article.")]
        public async Task WikiCmd([Summary("The search term."), Remainder] string term)
        {
            try
            {
                var data = await _net.GetWikipediaArticleAsync(term);
                if (data.Item1 == "" && data.Item2 == "")
                    await ReplyAsync("Failed to find the article.");
                else
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithTitle(data.Item1)
                        .WithUrl(data.Item2)
                        .WithDescription("Click the title to view the article!")
                        .WithColor(_misc.RandomColor())
                    .Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}