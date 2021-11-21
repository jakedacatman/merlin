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
    public class SpeedUpCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public SpeedUpCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("speedup")]
        [Alias("su", "speed")]
        [Summary("Speeds up a GIF.")]
        public async Task SpeedUpAsync([Summary("The speed multiplier.")] double speed, [Summary("The image to change the speed of.")] string url = null)
        {
            url = await _img.ParseUrlAsync(url, Context.Message);
            var img = await _img.SpeedUpAsync(url, speed);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
            img.Dispose();
        }
    }
}