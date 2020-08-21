using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Image")]
    public class DemotivationalCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public DemotivationalCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("demotivational")]
        [Alias("de", "dm")]
        [Summary("Creates a demotivational poster from an image and some text.")]
        public async Task DemotivationalCmd([Summary("The text to write.")]string text, [Summary("The text to put above.")]string title = null, [Summary("The image to create a poster of.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context);
                if (await _img.IsVideoAsync(url))
                {
                    var path = await _img.VideoFilter(url, _img.Demotivational, title, text);
                    await _img.SendToChannelAsync(path, Context.Channel);
                }
                else
                {
                    var img = await _img.Demotivational(url, title, text);
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