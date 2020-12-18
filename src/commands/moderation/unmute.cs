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
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [Summary("Unmutes a user.")]
        public async Task UnmuteCmd([Summary("The user to unmute.")] SocketGuildUser user)
        {
            try
            {
                if (await _mod.TryUnmuteUserAsync(Context.Guild, user))
                    await ReplyAsync("Consider it done.");
                else
                    await ReplyAsync("Failed to unmute the user.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}