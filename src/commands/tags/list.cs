using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using System.Linq;
using Fergun.Interactive;

namespace merlin.commands
{
    public partial class TagCommand : ModuleBase<ShardedCommandContext>
    {
        [Command("list")]
        [Summary("Gets all tags for the current guild.")]
        [Priority(1)]
        public async Task ListAsync()
        {
            var tags = _db.GetTags(Context.Guild.Id);
            if (!tags.Any())
            {
                await ReplyAsync("No tags found.");
                return;
            }

            var pages = new List<PageBuilder>();

            var numberedTags = tags.Select((x, i) => $"{i + 1}. {x}").Chunk(10);

            for (int i = 0; i < numberedTags.Count(); i++)
            {
                var chunk = numberedTags.ElementAt(i);

                pages.Add(new PageBuilder()
                    .WithTitle("Commands")
                    .WithFields(new EmbedFieldBuilder().WithName($"#{i * 10 + 1}{(chunk.Count() == 1 ? "" : $" to #{i * 10 + chunk.Count()}")}").WithValue(string.Join("\n", chunk)))
                    .WithColor(_rand.RandomColor()));
            }

            var msg = await _misc.SendPaginatorAsync(Context.User, pages, Context.Channel);

            if (msg.IsCanceled) await msg.Message.DeleteAsync();
        }
    }
}