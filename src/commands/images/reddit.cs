using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using donniebot.classes;
using Interactivity;

namespace donniebot.commands
{
    [Name("Image")]
    public class RedditCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly ImageService _img;
        private readonly RandomService _rand;
        private readonly NetService _net;

        private Regex _reg = new Regex(@"[0-9a-z]{1,21}"); 

        public RedditCommand(DiscordShardedClient client, MiscService misc, ImageService img, RandomService rand, NetService net)
        {
            _client = client;
            _misc = misc;
            _img = img;
            _rand = rand;
            _net = net;
        }

        [Command("reddit")]
        [Alias("red", "rd")]
        [Summary("Grabs a random image from Reddit.")]
        public async Task RedditCmd([Summary("The subreddit to pull from.")]string sub, [Summary("The optional sort mode in lowercase. Accepts \"top\", \"best\", \"new\", \"rising\", \"hot\", and \"controversial\".")]string mode = "top")
        {
            try
            {
                if (_reg.Match(sub).Success)
                {
                    var img = await _img.GetRedditImageAsync(sub, Context.Guild.Id, false, mode);
                    if (img.Url == null)
                    {
                        await ReplyAsync("An image could not be located.");
                        return;
                    }

                    var embed = new EmbedBuilder()
                        .WithTitle(img.Title)
                        .WithColor(_rand.RandomColor())
                        .WithTimestamp(DateTime.UtcNow)
                        .WithImageUrl(img.Url)
                        .WithFooter($"Posted by {img.Author} • From {img.Subreddit}");

                        
                    await ReplyAsync(embed: embed.Build());
                }
                else
                    await ReplyAsync("Invalid subreddit.");
            }
            catch (System.Net.Http.HttpRequestException)
            {
                await ReplyAsync("Invalid subreddit.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}