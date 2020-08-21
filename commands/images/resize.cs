using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Image")]
    public class ResizeCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public ResizeCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("resize")]
        [Alias("r", "re")]
        [Summary("Resizes an image.")]
        public async Task ResizeCmd([Summary("The width to change the size to.")] int width, [Summary("The height to change the size to.")] int height, [Summary("The image to change.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context);
                if ((width > 0 && height > 0) && (width <= 2000 && height <= 2000))
                {
                    var img = await _img.Resize(url.Trim('<').Trim('>'), width, height);
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