using System;
using Discord;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using merlin.services;

namespace merlin.commands
{
    [Name("Moderation")]
    public class PurgeCommand : ModuleBase<ShardedCommandContext>
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
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.ManageMessages | GuildPermission.ManageChannels)]
        [Summary("Deletes up to 1000 messages from the current channel, or 100 from a user in the current channel.")]
        public async Task PurgeAsync([Summary("The amount of messages to delete.")] int count = 100)
        {            
            await Context.Message.DeleteAsync();
            var ct = await _mod.TryPurgeMessagesAsync(Context.Channel as SocketTextChannel, count);
            if (ct > 0)
                await ReplyAsync($"Purged {ct} {(ct > 1 ? "messages" : "message")}.");
            else
                await ReplyAsync("Failed to purge the channel.");
        }

        [Command("purge")]
        [Alias("prune")]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.ManageMessages | GuildPermission.ManageChannels)]
        [Summary("Deletes up to 100 messages from a user in the current channel.")]
        public async Task PurgeAsync([Summary("The user to purge messages from.")]SocketGuildUser user, [Summary("The amount of messages to delete.")] int count = 100)
        {
            await Context.Message.DeleteAsync();
            var ct = await _mod.TryPurgeMessagesAsync(Context.Channel as SocketTextChannel, count, user);
            if (ct > 0)
                await ReplyAsync($"Purged {ct} {(ct > 1 ? "messages" : "message")} from `{user}`.");
            else
                await ReplyAsync("Failed to purge the channel.");
        }
    }
}