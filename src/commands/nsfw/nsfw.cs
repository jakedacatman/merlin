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
    [Name("Nsfw")]
    public class NsfwCommand : ModuleBase<ShardedCommandContext>
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
        public async Task NsfwAsync([Summary("The optional subreddit to pull from.")]string sub = null, [Summary("The optional sort mode in lowercase. Accepts the same modes as the reddit command.")]string mode = "top")
        {
            try
            {
                GuildImage img;
                if (sub == null)
                    img = await _img.GetRedditImageAsync(Context.Guild.Id, "nsfw", true, mode);
                else if (_reg.Match(sub).Success)
                    img = await _img.GetRedditImageAsync(sub, Context.Guild.Id, true, mode);
                else
                {
                    await ReplyAsync("Invalid subreddit.");
                    return;
                }

                if (img.Url == null)
                {
                    await ReplyAsync("An image could not be located.");
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithTitle(img.Title)
                    .WithColor(_rand.RandomColor())
                    .WithTimestamp(DateTime.UtcNow)
                    .WithFooter($"Posted by {img.Author} • From {img.Subreddit}");

                if (img.Type == "image")
                    embed = embed.WithImageUrl(img.Url);
                else
                {
                    embed = embed
                        .WithUrl(img.Url)
                        .WithDescription("Click the title to see the soundless video\nFor audio, replace the number in the URL (example: `720`) with `audio`.");
                }
                        
                await ReplyAsync(embed: embed.Build());
            }
            catch(System.Net.Http.HttpRequestException)
            {
                await ReplyAsync("Invalid subreddit.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessageAsync(e)).Build());
            }
        }
    }
}