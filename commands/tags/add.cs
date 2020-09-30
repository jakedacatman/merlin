using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    public partial class TagCommand : InteractiveBase<ShardedCommandContext>
    {
        [Command("add")]
        [Summary("Adds the specified tag.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Priority(1)]
        public async Task AddCmd([Summary("The name of the tag.")] string tag, [Summary("The value of the tag."), Remainder] string value)
        {
            try
            {
                if (tag.Length > 100)
                {
                    await ReplyAsync("Tag is too long. Limit it to 100 characters or less.");
                    return;
                }
                if (value.Length > 1000)
                {
                    await ReplyAsync("Value is too long. Limit it to 1000 characters or less.");
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
                        var reply = await NextMessageAsync(timeout: TimeSpan.FromSeconds(10));

                        if (reply.Content.ToLower() == "yes" || reply.Content.ToLower() == "y")
                        {
                            _db.RemoveTag(tag, Context.Guild.Id);
                            ct = _db.AddTag(tag, value, Context.Guild.Id);

                            if (!ct) await ReplyAsync("Failed to add the tag.");
                        }
                        
                        await msg.DeleteAsync();
                        await reply.DeleteAsync();
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