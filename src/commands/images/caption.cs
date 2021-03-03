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
    public class CaptionCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;
        private readonly NetService _net;

        public CaptionCommand(DiscordShardedClient client, ImageService img, MiscService misc, NetService net)
        {
            _client = client;
            _img = img;
            _misc = misc;
            _net = net;
        }

        [Command("caption")]
        [Alias("c", "cap")]
        [Summary("Captions an image.")]
        public async Task CaptionCmd([Summary("The text to caption.")]string text, [Summary("The image to caption.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context.Message);
                if (await _net.IsVideoAsync(url))
                {
                    var path = await _img.VideoFilter(url, _img.Caption, text);
                    await _img.SendToChannelAsync(path, Context.Channel, new MessageReference(Context.Message.Id));
                }
                else
                {
                    var img = await _img.Caption(url, text);
                    await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
                }
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}