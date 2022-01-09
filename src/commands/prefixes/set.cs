using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using System.Linq;
using donniebot.classes;
using donniebot.services;
using Discord.WebSocket;

namespace donniebot.commands
{
    [Name("Prefix")]
    [Group("prefix")]
    public class SetCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly DbService _db;
        private readonly InteractiveService _inter;

        public SetCommand(MiscService misc, DbService db, InteractiveService inter)
        {
            _misc = misc;
            _db = db;
            _inter = inter;
        }

        [Command("set")]
        [Alias("change")]
        [Summary("Changes the prefix to the specified string.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SetAsync([Summary("The new prefix."), Remainder] string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                var msg = await ReplyAsync($"My current prefix is {_db.GetPrefix(Context.Guild.Id).Prefix}`. Did you intend to reset it?", 
                    components: new ComponentBuilder()
                        .WithButton("Confirm", "confirm")
                        .WithButton("Cancel", "cancel", ButtonStyle.Danger)
                        .Build()
                );

                var interaction = await _inter.NextMessageComponentAsync(x => x.User == Context.User && x.Message.Id == msg.Id, timeout: TimeSpan.FromSeconds(10));

                if (interaction.IsSuccess && interaction.Value.Data.CustomId == "confirm")
                {
                    _db.RemovePrefix(Context.Guild.Id);
                    await ReplyAsync("The prefix has been reset to default; mention me if you are unsure of what that is.");
                }
                    
                await msg.DeleteAsync();

                return;
            }
            
            var gp = new GuildPrefix { GuildId = Context.Guild.Id, Prefix = prefix };

            _db.RemovePrefix(Context.Guild.Id);
            var suc = _db.AddItem<GuildPrefix>("prefixes", gp);
            if (suc) 
                await ReplyAsync($"Changed the prefix to `{prefix}`.");
            else 
                await ReplyAsync("Failed to change the prefix.");
        }
    }
}