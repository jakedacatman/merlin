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
    public class ResetCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly DbService _db;
        private readonly InteractivityService _inter;

        public ResetCommand(MiscService misc, DbService db, InteractivityService inter)
        {
            _misc = misc;
            _db = db;
            _inter = inter;
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