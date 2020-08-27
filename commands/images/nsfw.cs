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
    public class NsfwCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly ImageService _img;
        private readonly RandomService _rand;

        private Regex _reg = new Regex(@"[0-9a-z]{1,21}"); 

        public NsfwCommand(DiscordShardedClient client, MiscService misc, ImageService img, RandomService rand)
        {
            _client = client;
            _misc = misc;
            _img = img;
            _rand = rand;
        }

        [Command("nsfw")]
        [Alias("n")]
        [Summary("Grabs a random NSFW image from Reddit.")]
        [RequireNsfw]
        public async Task NsfwCmd([Summary("The optional subreddit to pull from.")]string sub = null, [Summary("The optional sort mode in lowercase.")]string mode = "top")
        {
            try
            {
                Dictionary<string, string> info;
                if (sub == null)
                    info = await _img.GetRedditImageAsync(Context.Guild.Id, "nsfw", true, mode);
                else if (_reg.Match(sub).Success)
                    info = await _img.GetRedditImageAsync(sub, Context.Guild.Id, true, mode);
                else
                {
                    await ReplyAsync("Invalid subreddit.");
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithTitle(info["title"])
                    .WithColor(_rand.RandomColor())
                    .WithTimestamp(DateTime.UtcNow)
                    .WithFooter($"Posted by {info["author"]} • From {info["sub"]}");

                if (info["type"] == "image")
                    embed = embed.WithImageUrl(info["url"]);
                else
                {
                    embed = embed
                        .WithUrl(info["url"])
                        .WithDescription("Click the title to see the soundless video\nFor audio, replace the number in the URL (example: `720`) with `audio`.");
                }
                        
                await ReplyAsync(embed: embed.Build());
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