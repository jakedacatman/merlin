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
    public class BlurCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public BlurCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("blur")]
        [Alias("bl")]
        [Summary("Blurs an image.")]
        public async Task BlurAsync([Summary("The amount to blur.")] float amount = 1,[Summary("The image to blur.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context.Message);
                var img = await _img.BlurAsync(url, amount);
                await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessageAsync(e)).Build());
            }
        }
    }
}