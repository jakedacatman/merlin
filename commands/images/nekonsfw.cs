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
    public class NekoNsfwCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly ImageService _img;

        public NekoNsfwCommand(DiscordShardedClient client, MiscService misc, ImageService img)
        {
            _client = client;
            _misc = misc;
            _img = img;
        }

        [Command("nekonsfw")]
        [Alias("nen", "nekon", "nekn")]
        [RequireNsfw]
        [Summary("Grabs an NSFW image from the nekos.life API.")]
        public async Task RedditCmd([Summary("The endpoint to pull from.")]string ep = "nsfw_neko_gif")
        {
            try
            {
                var info = await _img.GetNekoImageAsync(true, Context.Guild.Id, ep);
                if (info.Substring(0, 5) == "NSFW:")
                    await ReplyAsync(info);
                else
                    await ReplyAsync(embed: (new EmbedBuilder()
                        .WithColor(_misc.RandomColor())
                        .WithImageUrl(info)
                        .WithTimestamp(DateTime.UtcNow)
                    ).Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}