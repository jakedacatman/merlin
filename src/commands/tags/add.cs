using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Selection;
using merlin.classes;
using Discord.WebSocket;
using System.Linq;

namespace merlin.commands
{
    public partial class TagCommand : ModuleBase<ShardedCommandContext>
    {
        [Command("add")]
        [Summary("Adds the specified tag.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Priority(1)]
        public async Task AddAsync([Summary("The name of the tag.")] string tag, [Summary("The value of the tag."), Remainder] string value = null)
        {
            if (value == null)
                if (!Context.Message.Attachments.Any())
                    value = (await _misc.GetPreviousMessageAsync(Context.Channel as SocketTextChannel)).Content;
                else value = Context.Message.Attachments.First().Url;
            
            if (tag.Length > 150)
            {
                await ReplyAsync("Tag is too long. Limit it to 150 characters or less.");
                return;
            }
            if (value.Length > 1500)
            {
                await ReplyAsync("Value is too long. Limit it to 1500 characters or less.");
                return;
            }
            if (string.IsNullOrWhiteSpace(tag))
            {
                await ReplyAsync("The tag is empty.");
                return;
            }
            if (string.IsNullOrWhiteSpace(value))
            { 
                await ReplyAsync("The value is empty.");
                return;
            }

            var ct = _db.AddTag(tag, value, Context.Guild.Id);
            if (ct)
                await ReplyAsync($"Added tag `{tag}`.");
            else
            {
                if (_db.GetTag(tag, Context.Guild.Id) == null) await ReplyAsync("Failed to add the tag.");
                else
                {

                    var interaction = await _inter.SendSelectionAsync(new ButtonSelectionBuilder<string>()
                        .AddUser(Context.User)
                        .WithSelectionPage(new PageBuilder()
                            .WithTitle("⚠️ Warning ⚠️")
                            .WithDescription($"A tag already exists with the name \"{tag}\". Replace it?")
                            .WithCurrentTimestamp()
                            .WithColor(_rand.RandomColor()
                        ))
                        .WithInputType(InputType.Buttons)
                        .WithOptions(new[] { new ButtonOption<string>("Confirm", ButtonStyle.Primary), new ButtonOption<string>("Cancel", ButtonStyle.Danger) })
                        .Build(), Context.Channel, timeout: TimeSpan.FromSeconds(10)); 

                    if (interaction.IsSuccess && interaction.Value.Option == "Confirm")
                    {
                        _db.RemoveTag(tag, Context.Guild.Id);
                        ct = _db.AddTag(tag, value, Context.Guild.Id);

                        if (!ct) await ReplyAsync("Failed to add the tag.");
                        else await ReplyAsync($"Added tag `{tag}`.");
                    }
                    
                    await interaction.Message.DeleteAsync();
                }
            }
        }
    }
}