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
    public class SaturateCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public SaturateCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("saturate")]
        [Alias("sa")]
        [Summary("Saturates an image.")]
        public async Task SaturateAsync([Summary("The amount to saturate.")] float amount = 3, [Summary("The image to saturate.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context.Message);
                var img = await _img.SaturateAsync(url, amount);
                await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessageAsync(e)).Build());
            }
        }
    }
}