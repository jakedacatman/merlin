using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Interactivity;
using Discord.WebSocket;
using System.Linq;
using donniebot.classes;
using donniebot.services;
using LiteDB;

namespace donniebot.commands
{
    [Name("Prefix")]
    [Group("prefix")]
    public class PrefixCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly DbService _db;
        private readonly InteractivityService _inter;
        private readonly GuildPrefix _default;

        public PrefixCommand(MiscService misc, DbService db, InteractivityService inter, GuildPrefix def)
        {
            _misc = misc;
            _db = db;
            _inter = inter;
            _default = def;
        }

        [Command("")]
        [Alias("get")]
        [Summary("Gets the current prefix.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PrefixAsync()
        {
            try
            {
                await ReplyAsync($" My current prefix is `{_db.GetPrefix(Context.Guild.Id).Prefix ?? _default.Prefix}`.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessageAsync(e)).Build());
            }
        }
    }
}