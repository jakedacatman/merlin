using System;
using Discord;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Moderation")]
    public class PurgeCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ModerationService _mod;
        private readonly MiscService _misc;

        public PurgeCommand(DiscordShardedClient client, ModerationService mod, MiscService misc)
        {
            _client = client;
            _mod = mod;
            _misc = misc;
        }

        [Command("purge")]
        [Alias("prune")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Deletes up to 1000 messages from the current channel.")]
        public async Task PurgeCmd([Summary("The amount of messages to delete.")] int count = 100)
        {
            try
            {
                var ct = await _mod.TryPurgeMessagesAsync(Context.Channel as SocketTextChannel, count);
                if (ct > 0)
                    await ReplyAsync($"Purged {ct} {(ct > 1 ? "messages" : "message")}.");
                else
                    await ReplyAsync("Failed to purge the channel (purged 0 messages).");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }

        [Command("purge")]
        [Alias("prune")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Summary("Deletes up to 1000 messages from a user in the current channel.")]
        public async Task PurgeCmd([Summary("The user to purge messages from.")]SocketGuildUser user, [Summary("The amount of messages to delete.")] int count = 100)
        {
            try
            {
                var ct = await _mod.TryPurgeMessagesAsync(Context.Channel as SocketTextChannel, count, user);
                if (ct > 0)
                    await ReplyAsync($"Purged {ct} {(ct > 1 ? "messages" : "message")}.");
                else
                    await ReplyAsync("Failed to purge the channel.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}