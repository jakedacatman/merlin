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
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [Summary("Mutes a user.")]
        public async Task MuteCmd([Summary("The user to mute.")] SocketGuildUser user)
        {
            try
            {
                if (await _mod.TryMuteUserAsync(Context.Guild, (Context.User as SocketGuildUser), user))
                    await ReplyAsync("Consider it done.");
                else
                    await ReplyAsync("Failed to mute the user.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}