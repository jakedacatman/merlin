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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace donniebot.commands
{
    [Name("Image")]
    public class DrawTextCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public DrawTextCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("drawtext")]
        [Alias("d")]
        [Summary("Draws text on an image.")]
        public async Task DrawTextCmd([Summary("The text to draw.")]string text, [Summary("The optional bottom text to draw.")] string bottomText = null, [Summary("The image to modify.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context);
                if (await _img.IsVideoAsync(url))
                {
                    var path = await _img.VideoFilter(url, _img.DrawText, text, bottomText);
                    await _img.SendToChannelAsync(path, Context.Channel);
                }
                else
                {
                    var img = await _img.DrawText(url, text, bottomText);
                    await _img.SendToChannelAsync(img, Context.Channel);
                }
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}