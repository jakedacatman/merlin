using System;
using System.Collections.Generic;   
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;
using System.IO;
using SixLabors.ImageSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using donniebot.classes;

namespace donniebot.commands
{
    [Name("Image")]
    public class RedditCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly ImageService _img;

        private Regex _reg = new Regex(@"[0-9a-z]{1,21}"); 

        public RedditCommand(DiscordShardedClient client, MiscService misc, ImageService img)
        {
            _client = client;
            _misc = misc;
            _img = img;
        }

        [Command("reddit")]
        [Alias("red", "rd")]
        [Summary("Grabs a random image from Reddit.")]
        public async Task RedditCmd([Summary("The subreddit to pull from.")]string sub = null, [Summary("The optional sort mode in lowercase.")]string mode = "top")
        {
            try
            {
                if (_reg.Match(sub).Success)
                {
                    var info = await _img.GetRedditImageAsync(sub, Context.Guild.Id, false, mode);
                    await ReplyAsync(embed: (new EmbedBuilder()
                        .WithTitle(info["title"])
                        .WithColor(_misc.RandomColor())
                        .WithImageUrl(info["url"])
                        .WithTimestamp(DateTime.UtcNow)
                        .WithFooter($"Posted by {info["author"]} • From {info["sub"]}")
                    ).Build());
                }
                else
                {
                    await ReplyAsync("Invalid subreddit.");
                }
            }
            catch (ArgumentNullException)
            {
                await ReplyAsync("An image could not be located.");
            }
            catch (Exception e) when (e.Message == "There are no more images.")
            {
                await ReplyAsync(e.Message);
            }
            catch(System.Net.Http.HttpRequestException)
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