using System;
using Discord;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;

namespace merlin.commands
{
    [Name("Moderation")]
    public class MuteCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ModerationService _mod;
        private readonly MiscService _misc;

        public MuteCommand(DiscordShardedClient client, ModerationService mod, MiscService misc)
        {
            _client = client;
            _mod = mod;
            _misc = misc;
        }

        [Command("mute")]
        [Alias("timeout", "to")]
        [RequireBotPermission(GuildPermission.MuteMembers | GuildPermission.ManageRoles)]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [Summary("Mutes a user.")]
        public async Task MuteAsync([Summary("The user to mute.")] SocketGuildUser user, [Summary("The length of time to mute them for (max of 28 days.)")] TimeSpan? period = null, [Summary("The reason for muting them."), Remainder] string reason = null)
        {
            if (period > TimeSpan.FromDays(28))
            {
                await ReplyAsync("Timeouts can only last for 28 days.");
                return;
            }

            var res = await _mod.TryMuteUserAsync((Context.User as SocketGuildUser), user, reason, period);
            
            if (res.IsSuccess)
            {
                var msg = $"Consider it done, {Context.User.Mention}.";
                if (period is not null) msg += $" They will be unmuted at {res.Action.Expiry:dd/MM/yyyy HH:mm:ss UTC}.";

                await ReplyAsync(msg);
            }
            else
                await ReplyAsync($"Failed to mute the user: `{res.Message}`");
        }
    }
}