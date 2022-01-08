using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord;

namespace donniebot.commands
{
    [Name("Image")]
    public class OsamaCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public OsamaCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("osama")]
        [Alias("os")]
        [Summary("Makes Osama bin Laden watch something.")]
        public async Task OsamaAsync([Summary("The image to have him watch.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var osamaUrl = "https://i.jakedacatman.me/UfLp7.jpg";
            var img = await _img.OverlayAsync(osamaUrl, url, 103, 58, 80, 60, 4f);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}