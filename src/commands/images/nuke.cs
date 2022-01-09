using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;
using Discord;

namespace merlin.commands
{
    [Name("Image")]
    public class NukeCommand : ModuleBase<ShardedCommandContext>
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
        public async Task NukeAsync([Summary("The image to nuke.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var img = await _img.BackgroundColorAsync(url, _rand.RandomNumber(0, 255), _rand.RandomNumber(0, 255), _rand.RandomNumber(0, 255));
            _img.Saturate(img, _rand.RandomFloat(1, 5));
            _img.Brightness(img, _rand.RandomFloat(1, 5));
            _img.Blur(img, _rand.RandomFloat(1, 5));
            _img.Pixelate(img, _rand.RandomNumber(1, 4));
            _img.Sharpen(img, _rand.RandomFloat(1, 5));
            img = _img.Jpeg(img, _rand.RandomNumber(1, 15));
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}