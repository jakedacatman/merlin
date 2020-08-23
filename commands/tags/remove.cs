using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    public partial class TagCommand : InteractiveBase<ShardedCommandContext>
    {
        [Command("remove")]
        [Summary("Removes the specified tag.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Priority(1)]
        public async Task RemoveCmd([Summary("The name of the tag.")] string tag)
        {
            try
            {
                var ct = _db.RemoveTag(tag, Context.Guild.Id);
                if (ct > 0)
                    await ReplyAsync($"Deleted tag `{tag}`.");
                else
                    await ReplyAsync("Didn't find a tag to delete.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}