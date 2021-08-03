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
        private readonly DbService _db;
        private readonly InteractivityService _inter;

        public DjRoleCommand(DbService db, InteractivityService inter)
        {
            _db = db;
            _inter = inter;
        }

        [Command("djrole")]
        [Alias("dj")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        [Summary("Changes the DJ role to the specified role.")]
        public async Task DjRoleAsync([Summary("The new role."), Remainder] SocketRole role = null)
        {
            if (role == null)
            {
                var res = await _inter.NextMessageAsync(timeout: TimeSpan.FromSeconds(10)); 
                if (res.IsSuccess && (res.Value.Content.ToLower().Contains("yes") || res.Value.Content.ToLower() == "y"))
                {
                    _db.RemoveItems<DjRole>("djroles", Query.EQ("GuildId", Context.Guild.Id));
                    await ReplyAsync("Unbound the DJ role.");
                }
                else
                {
                    _inter.DelayedSendMessageAndDeleteAsync(Context.Channel, deleteDelay: TimeSpan.FromSeconds(10), text: "The DJ role was not unbound.");
                }
                return;
            }
            
            var djrole = new DjRole(Context.Guild.Id, role.Id);

            var suc = _db.AddItem<DjRole>("djroles", djrole);
            if (suc) 
                await ReplyAsync($"Changed the DJ role to {role.Mention}.", allowedMentions: AllowedMentions.None);
            else 
                await ReplyAsync("Failed to change the DJ role.");
        }
    }
}