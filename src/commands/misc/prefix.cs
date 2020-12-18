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
    public class PrefixCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly DbService _db;

        public PrefixCommand(MiscService misc, DbService db)
        {
            _misc = misc;
            _db = db;
        }

        [Command("prefix")]
        [Summary("Changes the prefix to the specified string.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PrefixCmd([Summary("The new prefix."), Remainder] string prefix = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    _db.RemoveItems<GuildPrefix>("prefixes", Query.Where("GuildId", x => x.AsDouble == (double)Context.Guild.Id));
                    await ReplyAsync("Reset the prefix to default.");
                    return;
                }
                
                var gp = new GuildPrefix { GuildId = Context.Guild.Id, Prefix = prefix };

                var suc = _db.AddItem<GuildPrefix>("prefixes", gp);
                if (suc) 
                    await ReplyAsync($"Changed the prefix to `{prefix}`.");
                else 
                    await ReplyAsync("Failed to change the prefix.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}