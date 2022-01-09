using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;
using Discord;

namespace merlin.commands
{
    [Name("Image")]
    public class VideoToGifCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public VideoToGifCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("videotogif")]
        [Alias("vtg", "v2g")]
        [Summary("Converts a video to a GIF.")]
        public async Task VideoToGifAsync([Summary("The video to convert.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var path = await _img.VideoToGifAsync(url);
            await _img.SendToChannelAsync(path, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}