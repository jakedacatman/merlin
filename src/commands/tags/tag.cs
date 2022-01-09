using System;
using System.Threading.Tasks;
using Discord.Commands;
using Fergun.Interactive;
using merlin.services;

namespace merlin.commands
{
    [Name("Tag")]
    [Group("tag")]
    public partial class TagCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly DbService _db;
        private readonly InteractiveService _inter;
        private readonly RandomService _rand;

        public TagCommand(MiscService misc, DbService db, InteractiveService inter, RandomService rand)
        {
            _misc = misc;
            _db = db;
            _inter = inter;
            _rand = rand;
        }

        [Command("")]
        [Summary("Gets the specified tag.")]
        [Priority(0)]
        public async Task TagAsync([Summary("The name of the tag."), Remainder] string tag)
        {
            var found = _db.GetTag(tag, Context.Guild.Id);
            if (found != null)
                await Context.Channel.SendMessageAsync(found.Value, allowedMentions: Discord.AllowedMentions.None);
            else
                await ReplyAsync("Failed to find tag.");
        }
    }
}