using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;
using Discord;

namespace merlin.commands
{
    [Name("Image")]
    public class DrawCommand : ModuleBase<ShardedCommandContext>
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
        [Summary("Try it and see.")]
        public async Task DrawAsync([Summary("The user.")] SocketGuildUser user = null)
        {
            if (user == null) user = Context.User as SocketGuildUser;

            string url = await _img.ParseUrlAsync(user.GetAvatarUrl(size: 1024), Context.Message);
            var img = await _img.PlaceBelowAsync("https://i.jakedacatman.me/9JPyB.png", url);
            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}