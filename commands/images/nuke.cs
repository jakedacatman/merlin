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
    public class NukeCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public NukeCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("nuke")]
        [Alias("nu")]
        [Summary("Nukes an image.")]
        public async Task NukeCmd([Summary("The image to nuke.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context);
                var img = await _img.BackgroundColor(url, _misc.RandomNumber(0, 255), _misc.RandomNumber(0, 255), _misc.RandomNumber(0, 255));
                img = _img.Saturate(img, _misc.RandomFloat(5));
                img = _img.Brightness(img, _misc.RandomFloat(5, 1));
                img = _img.Blur(img, _misc.RandomFloat(5));
                img = _img.Pixelate(img, _misc.RandomNumber(1, 4));
                img = _img.Sharpen(img, _misc.RandomFloat(5));
                img = _img.Jpeg(img, _misc.RandomNumber(1, 15));
                await _img.SendToChannelAsync(img, Context.Channel);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}