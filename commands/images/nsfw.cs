using System;
using System.Text.RegularExpressions;
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

        private Regex _reg = new Regex(@"[0-9a-z]{1,21}"); 

        public NsfwCommand(DiscordShardedClient client, MiscService misc, ImageService img)
        {
            _client = client;
            _misc = misc;
            _img = img;
        }

        [Command("nsfw")]
        [Alias("n")]
        [Summary("Grabs a random NSFW image from Reddit.")]
        [RequireNsfw]
        public async Task NsfwCmd([Summary("The optional subreddit to pull from.")]string sub = null, [Summary("The optional sort mode in lowercase.")]string mode = "top")
        {
            try
            {
                if (sub == null)
                {
                    var info = await _img.GetRedditImageAsync(Context.Guild.Id, "nsfw", true, mode);
                    await ReplyAsync(embed: (new EmbedBuilder()
                        .WithTitle(info["title"])
                        .WithColor(_misc.RandomColor())
                        .WithImageUrl(info["url"])
                        .WithTimestamp(DateTime.UtcNow)
                        .WithFooter($"Posted by {info["author"]} • From {info["sub"]}")
                    ).Build());
                }
                else if (_reg.Match(sub).Success)
                {
                    var info = await _img.GetRedditImageAsync(sub, Context.Guild.Id, true, mode);
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