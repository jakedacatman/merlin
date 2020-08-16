using System;
using System.Collections.Generic;   
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;
using System.IO;
using SixLabors.ImageSharp;

namespace donniebot.commands
{
    [Name("Image")]
    public class OsamaCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public OsamaCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("osama")]
        [Alias("os")]
        [Summary("Makes Osama bin Laden watch something.")]
        public async Task OverlayCmd([Summary("The image to have him watch.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context);
                var osamaUrl = await _img.ParseUrlAsync("https://i.jakedacatman.me/UfLp7.jpg", Context);
                var img = await _img.Overlay(osamaUrl, url, 106, 64, 73, 48, 4f);
                await _img.SendToChannelAsync(img, Context.Channel);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}