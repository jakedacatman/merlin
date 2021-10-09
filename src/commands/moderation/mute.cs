using System;
using Discord;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;

namespace donniebot.commands
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
        [RequireBotPermission(GuildPermission.MuteMembers | GuildPermission.ManageRoles)]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [Summary("Mutes a user.")]
        public async Task MuteAsync([Summary("The user to mute.")] SocketGuildUser user, [Summary("The length of time to mute them for.")] TimeSpan? period = null, [Summary("The reason for muting them.")] string reason = null)
        {
            var res = await _mod.TryMuteUserAsync(Context.Guild, (Context.User as SocketGuildUser), user, reason, period);
            
            if (res.IsSuccess)
            {
                var msg = $"Consider it done, {Context.User.Mention}.";
                if (period is not null) msg += $" They will be unmuted at {res.Action.Expiry:r}.";

                await ReplyAsync(msg);
            }
            else
                await ReplyAsync($"Failed to mute the user. (`{res.Message}`)");
        }
    }
}