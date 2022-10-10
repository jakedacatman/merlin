using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using System.Linq;
using merlin.classes;
using merlin.services;
using Discord.WebSocket;

namespace merlin.commands
{
    [Name("Prefix")]
    [Group("prefix")]
    public class SetCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly DbService _db;
        private readonly InteractiveService _inter;
        public readonly RandomService _rand;

        public SetCommand(MiscService misc, DbService db, InteractiveService inter, RandomService rand)
        {
            _misc = misc;
            _db = db;
            _inter = inter;
            _rand = rand;
        }

        [Command("set")]
        [Alias("change")]
        [Summary("Changes the prefix to the specified string.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task SetAsync([Summary("The new prefix."), Remainder] string prefix = null)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                var interaction = await _inter.SendSelectionAsync(new ButtonSelectionBuilder<string>()
                    .AddUser(Context.User)
                    .WithSelectionPage(new PageBuilder()
                        .WithTitle("⚠️ Warning ⚠️")
                        .WithDescription($"My current prefix is `{_db.GetPrefix(Context.Guild.Id).Prefix}`. Did you intend to reset it?")
                        .WithCurrentTimestamp()
                        .WithColor(_rand.RandomColor()
                    ))
                    .WithInputType(InputType.Buttons)
                    .WithOptions(new[] { new ButtonOption<string>("Confirm", ButtonStyle.Primary), new ButtonOption<string>("Cancel", ButtonStyle.Danger) })
                    .Build(), Context.Channel, timeout: TimeSpan.FromSeconds(10)); 

                if (interaction.IsSuccess && interaction.Value.Option == "Confirm")
                {
                    _db.RemovePrefix(Context.Guild.Id);
                    await ReplyAsync("The prefix has been reset to default; mention me if you are unsure of what that is.");
                }
                    
                await interaction.Message.DeleteAsync();
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