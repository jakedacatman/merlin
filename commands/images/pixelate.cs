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
    public class PixelateCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public PixelateCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("pixelate")]
        [Alias("p", "px")]
        [Summary("Changes an image's pixel size to the given size.")]
        public async Task PixelateCmd([Summary("The value to change the pixel size to.")] int size = 1,[Summary("The image to change.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context);
                var img = await _img.Pixelate(url.Trim('<').Trim('>'), size);
                await _img.SendToChannelAsync(img, Context.Channel);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}