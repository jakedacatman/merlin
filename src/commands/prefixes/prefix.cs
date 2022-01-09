using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using merlin.classes;
using merlin.services;

namespace merlin.commands
{
    [Name("Prefix")]
    [Group("prefix")]
    public class PrefixCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly DbService _db;
        private readonly GuildPrefix _default;

        public PrefixCommand(MiscService misc, DbService db, GuildPrefix def)
        {
            _misc = misc;
            _db = db;
            _default = def;
        }

        [Command("")]
        [Alias("get")]
        [Summary("Gets the current prefix.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PrefixAsync() => await ReplyAsync($" My current prefix is `{_db.GetPrefix(Context.Guild.Id)?.Prefix ?? _default.Prefix}`.");
    }
}