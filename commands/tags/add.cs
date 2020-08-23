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
        [Command("add")]
        [Summary("Adds the specified tag.")]
        [Priority(1)]
        public async Task AddCmd([Summary("The name of the tag.")] string tag, [Summary("The value of the tag."), Remainder] string value)
        {
            try
            {
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