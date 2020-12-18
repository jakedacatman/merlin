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
    public class RedditDownloadCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly ImageService _img;
        private readonly RandomService _rand;
        private readonly NetService _net;
        private readonly InteractivityService _inter;

        private Regex _reg = new Regex(@"[0-9a-z]{1,21}"); 

        public RedditDownloadCommand(DiscordShardedClient client, MiscService misc, ImageService img, RandomService rand, NetService net, InteractivityService inter)
        {
            _client = client;
            _misc = misc;
            _img = img;
            _rand = rand;
            _net = net;
            _inter = inter;
        }

        [Command("redditdownload")]
        [Alias("rdd", "rddl", "reddl")]
        [Summary("Downloads a video from Reddit.")]
        public async Task RedditDownloadCmd([Summary("The post to download from.")]string post)
        {
            try
            {
                var msg = await ReplyAsync("Downloading your video...");
                await _img.DownloadRedditVideoAsync(post, Context.Channel as SocketGuildChannel);
                _inter.DelayedSendMessageAndDeleteAsync(Context.Channel, deleteDelay: TimeSpan.FromSeconds(30), text: $"{Context.User.Mention}, your video is ready.");
            }
            catch (System.Net.Http.HttpRequestException e)
            {
                await ReplyAsync($"Downloading encountered an error: `{e.Message}`");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}