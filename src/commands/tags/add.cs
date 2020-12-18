using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Interactivity;
using Discord.WebSocket;
using System.Linq;

namespace donniebot.commands
{
    public partial class TagCommand : ModuleBase<ShardedCommandContext>
    {
        [Command("add")]
        [Summary("Adds the specified tag.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Priority(1)]
        public async Task AddCmd([Summary("The name of the tag.")] string tag, [Summary("The value of the tag."), Remainder] string value = null)
        {
            try
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
                        var msg = await ReplyAsync($"A tag already exists with the name \"{tag}\". Remove it?");
                        var reply = await _inter.NextMessageAsync(timeout: TimeSpan.FromSeconds(10));
                        
                        if (!reply.IsSuccess)
                        {
                            await msg.DeleteAsync();
                            return;
                        }
                        else
                            await reply.Value.DeleteAsync();

                        if (reply.Value.Content.ToLower() == "yes" || reply.Value.Content.ToLower() == "y")
                        {
                            _db.RemoveTag(tag, Context.Guild.Id);
                            ct = _db.AddTag(tag, value, Context.Guild.Id);

                            if (!ct) await ReplyAsync("Failed to add the tag.");
                            else 
                                await ReplyAsync($"Added tag `{tag}`.");
                        }
                        
                        await msg.DeleteAsync();
                    }
                }
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}