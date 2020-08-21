using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Image")]
    public class AvatarCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;

        public AvatarCommand(DiscordShardedClient client, ImageService img, MiscService misc)
        {
            _client = client;
            _img = img;
            _misc = misc;
        }

        [Command("avatar")]
        [Alias("a", "av")]
        [Summary("Gets a user's avatar.")]
        public async Task AvatarCmd([Summary("The user.")] SocketGuildUser user = null)
        {
            try
            {
                if (user == null) user = Context.User as SocketGuildUser;

                var url = user.GetAvatarUrl(size: 512);
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithColor(_misc.RandomColor())
                    .WithImageUrl(url)
                    .WithCurrentTimestamp()
                    .Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}