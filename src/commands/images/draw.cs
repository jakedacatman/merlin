using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Image")]
    public class DrawCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public DrawCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("draw")]
        [Summary("Surprise!")]
        public async Task BlurCmd([Summary("The user.")] SocketGuildUser user = null)
        {
            try
            {
                if (user == null) user = Context.User as SocketGuildUser;

                string url = await _img.ParseUrlAsync(user.GetAvatarUrl(size: 1024), Context.Message);
                var img = await _img.PlaceBelow("https://i.jakedacatman.me/9JPyB.png", url);
                await _img.SendToChannelAsync(img, Context.Channel);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}