using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;

namespace donniebot.commands
{
    [Name("Image")]
    public class AvatarCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public AvatarCommand(DiscordShardedClient client, ImageService img, MiscService misc, RandomService rand)
        {
            _client = client;
            _img = img;
            _misc = misc;
            _rand = rand;
        }

        [Command("avatar")]
        [Alias("a", "av")]
        [Summary("Gets a user's avatar.")]
        public async Task AvatarAsync([Summary("The user.")] SocketGuildUser user = null)
        {
            if (user == null) user = Context.User as SocketGuildUser;

            var url = user.GetAvatarUrl(size: 512);
            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(_rand.RandomColor())
                .WithImageUrl(url)
                .WithCurrentTimestamp()
                .Build(), messageReference: new MessageReference(Context.Message.Id), allowedMentions: AllowedMentions.None);
        }
        [Command("avatar")]
        [Alias("a", "av")]
        [Summary("Gets a user's avatar.")]
        public async Task AvatarAsync([Summary("The user.")] ulong userId)
        {
            var url = (await _client.Rest.GetUserAsync(userId))?.GetAvatarUrl(size: 512);

            if (url == null)
            {
                await ReplyAsync("Invalid user ID.");
                return;
            }

            await ReplyAsync(embed: new EmbedBuilder()
                .WithColor(_rand.RandomColor())
                .WithImageUrl(url)
                .WithCurrentTimestamp()
                .Build(), messageReference: new MessageReference(Context.Message.Id), allowedMentions: AllowedMentions.None);
        }
    }
}