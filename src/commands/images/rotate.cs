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
    public class RotateCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public RotateCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("rotate")]
        [Alias("ro", "rot")]
        [Summary("Rotates an image.")]
        public async Task RotateCmd([Summary("The amount in degrees to rotate.")] float deg, [Summary("The image to rotate.")] string url = null)
        {
            try
            {
                url = await _img.ParseUrlAsync(url, Context.Message);
                var img = await _img.RotateAsync(url, deg);
                await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}