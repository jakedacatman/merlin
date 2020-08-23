using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.Interactive;
using donniebot.classes;
using donniebot.services;
using LiteDB;

namespace donniebot.commands
{
    public partial class TagCommand : InteractiveBase<ShardedCommandContext>
    {
        [Command("remove")]
        [Summary("Removes the specified tag.")]
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