using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Image")]
    public class ImageInfoCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public ImageInfoCommand(DiscordShardedClient client, ImageService img, MiscService misc, RandomService rand)
        {
            _client = client;
            _img = img;
            _misc = misc;
            _rand = rand;
        }

        [Command("info")]
        [Alias("i", "inf")]
        [Summary("Gets some information about an image.")]
        public async Task ImageInfoCmd([Summary("The image to get the information for.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context.Message);
                var info = await _img.GetInfo(url);
                var em = new EmbedBuilder()
                    .WithColor(_rand.RandomColor())
                    .WithCurrentTimestamp()
                    .WithThumbnailUrl(url)
                    .WithFields(new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder().WithName("Width").WithValue(info["width"]).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Height").WithValue(info["height"]).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Resolution").WithValue($"{long.Parse(info["width"]) * long.Parse(info["height"])} pixels").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Frames").WithValue(info["frames"]).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Color depth").WithValue(info["bpp"] + "bpp").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Frames/second").WithValue((info["fps"] == "Infinity" ? "unknown " : info["fps"]) + "fps").WithIsInline(true),
                    });
                await Context.Channel.SendMessageAsync(embed: em.Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}