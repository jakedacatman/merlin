using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace merlin.commands
{
    public partial class TagCommand : ModuleBase<ShardedCommandContext>
    {
        [Command("remove")]
        [Summary("Removes the specified tag.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Priority(1)]
        public async Task RemoveAsync([Summary("The name of the tag."), Remainder] string tag)
        {
            var ct = _db.RemoveTag(tag, Context.Guild.Id);
            if (ct > 0)
                await ReplyAsync($"Deleted tag `{tag}`.");
            else
                await ReplyAsync("Didn't find a tag to delete.");
        }
    }
}