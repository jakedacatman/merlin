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
    public class OverlayCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public OverlayCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("overlay")]
        [Alias("o")]
        [Summary("Overlays an image on another image.")]
        public async Task OverlayCmd([Summary("The image to overlay.")] string overlayUrl, [Summary("The x-position to overlay to.")] int x = -1, [Summary("The y-position to overlay to.")] int y = -1, [Summary("The width of the overlaid image.")] int width = 0, [Summary("The height of the overlaid image.")] int height = 0, [Summary("The image to be overlaid upon.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context);
                overlayUrl = await _img.ParseUrlAsync(overlayUrl, Context);
                var img = await _img.Overlay(url, overlayUrl, x, y, width, height);
                await _img.SendToChannelAsync(img, Context.Channel);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}