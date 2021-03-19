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
    public class ScaleCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public ScaleCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }
        [Command("scale")]
        [Alias("sc")]
        [Summary("Scales an image.")]
        public async Task ScaleAsync([Summary("The scale to adjust the width (x-value) to.")] float xScale, [Summary("The scale to adjust the height (y-value) to.")] float yScale, [Summary("The image to scale.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context.Message);
                
                var img = await _img.ResizeAsync(url, xScale, yScale);
                await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessageAsync(e)).Build());
            }
        }
        [Command("scale")]
        [Alias("sc")]
        [Summary("Scales an image.")]
        public async Task ScaleAsync([Summary("The scale to adjust both width and height to.")] float scale, [Summary("The image to scale.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context.Message);
                
                var img = await _img.ResizeAsync(url, scale, scale);
                await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessageAsync(e)).Build());
            }
        }
    }
}