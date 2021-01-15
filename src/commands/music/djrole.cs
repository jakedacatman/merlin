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
    [Name("Music")]
    public class DjRoleCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly DbService _db;

        public DjRoleCommand(MiscService misc, DbService db)
        {
            _misc = misc;
            _db = db;
        }

        [Command("djrole")]
        [Alias("dj")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [Summary("Changes the DJ role to the specified role.")]
        public async Task DjRoleCmd([Summary("The new role."), Remainder] SocketRole role = null)
        {
            try
            {
                if (role == null)
                {
                    _db.RemoveItems<DjRole>("djroles", Query.EQ("GuildId", Context.Guild.Id));
                    await ReplyAsync("Reset the prefix to default.");
                    return;
                }
                
                var djrole = new DjRole(Context.Guild.Id, role.Id);

                var suc = _db.AddItem<DjRole>("djroles", djrole);
                if (suc) 
                    await ReplyAsync($"Changed the DJ role to {role.Mention}.", allowedMentions: AllowedMentions.None);
                else 
                    await ReplyAsync("Failed to change the DJ role.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}