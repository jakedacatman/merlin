using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Image")]
    public class NukeCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public NukeCommand(DiscordShardedClient client, ImageService img, MiscService misc, RandomService rand)
        {
            _client = client;
            _img = img;
            _misc = misc;
            _rand = rand;
        }

        [Command("nuke")]
        [Alias("nu")]
        [Summary("Nukes an image.")]
        public async Task NukeCmd([Summary("The image to nuke.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context.Message);
                var img = await _img.BackgroundColor(url, _rand.RandomNumber(0, 255), _rand.RandomNumber(0, 255), _rand.RandomNumber(0, 255));
                img = _img.Saturate(img, _rand.RandomFloat(1, 5));
                img = _img.Brightness(img, _rand.RandomFloat(1, 5));
                img = _img.Blur(img, _rand.RandomFloat(1, 5));
                img = _img.Pixelate(img, _rand.RandomNumber(1, 4));
                img = _img.Sharpen(img, _rand.RandomFloat(1, 5));
                img = _img.Jpeg(img, _rand.RandomNumber(1, 15));
                await _img.SendToChannelAsync(img, Context.Channel);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}