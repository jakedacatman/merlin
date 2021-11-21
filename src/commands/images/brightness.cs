using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;
using Discord;

namespace donniebot.commands
{
    [Name("Image")]
    public class BrightnessCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public BrightnessCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("brightness")]
        [Alias("br", "bright")]
        [Summary("Changes an image's brightness.")]
        public async Task BrightnessAsync([Summary("The value to change the brightness by.")] float brightness = 1,[Summary("The image to change.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var img = await _img.BrightnessAsync(url, brightness);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
            img.Dispose();
        }
    }
}