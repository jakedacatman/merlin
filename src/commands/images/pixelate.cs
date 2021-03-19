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
    public class PixelateCommand : ModuleBase<ShardedCommandContext>
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
        [Alias("px")]
        [Summary("Changes an image's pixel size to the given size.")]
        public async Task PixelateCmd([Summary("The value to change the pixel size to.")] int size = 1,[Summary("The image to change.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context.Message);
                var img = await _img.PixelateAsync(url, size);
                await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}