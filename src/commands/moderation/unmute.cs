using System;
using Discord;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;

namespace merlin.commands
{
    [Name("Moderation")]
    public class UnmuteCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ModerationService _mod;
        private readonly MiscService _misc;

        public UnmuteCommand(DiscordShardedClient client, ModerationService mod, MiscService misc)
        {
            _client = client;
            _mod = mod;
            _misc = misc;
        }

        [Command("unmute")]
        [RequireBotPermission(GuildPermission.MuteMembers | GuildPermission.ManageRoles)]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [Summary("Unmutes a user.")]
        public async Task UnmuteAsync([Summary("The user to unmute.")] SocketGuildUser user)
        {
            var res = await _mod.TryUnmuteUserAsync(user);
            
            if (res.IsSuccess)
                await ReplyAsync($"Consider it done, {Context.User.Mention}.");
            else
                await ReplyAsync($"Failed to unmute the user: `{res.Message}`");
        }
    }
}