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
    public class SpeedUpCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public SpeedUpCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("speedup")]
        [Alias("sp", "su")]
        [Summary("Speeds up a GIF.")]
        public async Task SpeedUpCmd([Summary("The speed to change the playback to (in times).")] int speed, [Summary("The image to change.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context);
                var img = await _img.SpeedUp(url.Trim('<').Trim('>'), speed);
                await _img.SendToChannelAsync(img, Context.Channel);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}