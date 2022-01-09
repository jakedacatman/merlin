using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using merlin.services;

namespace merlin.commands
{
    [Name("Prefix")]
    [Group("prefix")]
    public class ResetCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly DbService _db;

        public ResetCommand(MiscService misc, DbService db)
        {
            _misc = misc;
            _db = db;
        }

        [Command("reset")]
        [Summary("Resets the prefix to default.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ResetAsync()
        {
            _db.RemovePrefix(Context.Guild.Id);
            await ReplyAsync("The prefix has been reset to default; mention me if you are unsure of what that is.");
        }
    }
}