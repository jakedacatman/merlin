using System;
using System.Threading.Tasks;
using Discord.Commands;
using Interactivity;
using Discord;
using System.Linq;

namespace donniebot.commands
{
    public partial class TagCommand : ModuleBase<ShardedCommandContext>
    {
        [Command("list")]
        [Summary("Gets all tags for the current guild.")]
        [Priority(1)]
        public async Task ListCmd()
        {
            try
            {
                var tags = _db.GetTags(Context.Guild.Id);
                var toDisplay = tags.Any() ? string.Join(", ", tags) : " ";

                await ReplyAsync(embed: new EmbedBuilder()
                .WithTitle($"Tags for {Context.Guild.Name}:")
                .WithDescription($"```{toDisplay}```")
                .Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}