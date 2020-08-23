using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.Interactive;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Tag")]
    [Group("tag")]
    public partial class TagCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly DbService _db;

        public TagCommand(MiscService misc, DbService db)
        {
            _misc = misc;
            _db = db;
        }

        [Command("")]
        [Summary("Gets the specified tag.")]
        [Priority(0)]
        public async Task TagCmd([Summary("The name of the tag.")] string tag)
        {
            try
            {
                var found = _db.GetTag(tag, Context.Guild.Id);
                if (found != null)
                    await ReplyAsync(found.Value);
                else
                    await ReplyAsync("Failed to find tag.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}