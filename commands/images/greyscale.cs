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
    public class GreyscaleCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public GreyscaleCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("greyscale")]
        [Alias("gr", "grey", "gray", "grayscale")]
        [Summary("Makes an image greyscale.")]
        public async Task GreyscaleCmd([Summary("The image to convert to greyscale.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context);
                var img = await _img.Greyscale(url);
                await _img.SendToChannelAsync(img, Context.Channel);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}