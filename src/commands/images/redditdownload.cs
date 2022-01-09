using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;

namespace merlin.commands
{
    [Name("Image")]
    public class RedditDownloadCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly ImageService _img;
        private readonly RandomService _rand;
        private readonly NetService _net;

        private Regex _reg = new Regex(@"[0-9a-z]{1,21}"); 

        public RedditDownloadCommand(DiscordShardedClient client, MiscService misc, ImageService img, RandomService rand, NetService net)
        {
            _client = client;
            _misc = misc;
            _img = img;
            _rand = rand;
            _net = net;
        }

        [Command("redditdownload")]
        [Alias("rdd", "rddl", "reddl")]
        [Summary("Downloads a video from Reddit.")]
        public async Task RedditDownloadAsync([Summary("The post to download from.")] string post)
        {
            try
            {
                if (!Uri.IsWellFormedUriString(post, UriKind.Absolute) || !post.Contains("reddit.com"))
                {
                    await ReplyAsync("Invalid Reddit link; try a link to the original post.");
                    return;
                }

                var msg = await ReplyAsync("Downloading your video...");
                var c = Context.Channel as SocketGuildChannel;
                var succ = await _img.DownloadRedditVideoAsync(post, c, (c as SocketTextChannel).IsNsfw, new MessageReference(Context.Message.Id));
                if (!succ)
                    await ReplyAsync("Video failed to download. Was it really a video or GIF?");

                await msg.DeleteAsync();
            }
            catch (System.Net.Http.HttpRequestException e)
            {
                await ReplyAsync($"Downloading encountered an error: `{e.Message}`");
            }
        }
    }
}