using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;

namespace donniebot.commands
{
    [Name("Image")]
    public class SharpenCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public SharpenCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("sharpen")]
        [Alias("s")]
        [Summary("Changes an image's sharpness.")]
        public async Task SharpenAsync([Summary("The value to change the sharpness by.")] float sharpness = 1,[Summary("The image to change.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context.Message);
                var img = await _img.SharpenAsync(url, sharpness);
                await _img.SendToChannelAsync(img, Context.Channel, Context.Message.Reference);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessageAsync(e)).Build());
            }
        }
    }
}