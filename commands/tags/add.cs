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
                    await ReplyAsync("Failed to add the tag.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}