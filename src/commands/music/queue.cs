using System;
using Discord;
using System.Linq;
using Discord.Commands;
using donniebot.classes;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Interactivity;
using Interactivity.Pagination;

namespace donniebot.commands
{
    [Name("Music")]
    public class QueueCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly RandomService _rand;
        private readonly InteractivityService _inter;

        public QueueCommand(AudioService audio, RandomService rand, InteractivityService inter)
        {
            _audio = audio;
            _rand = rand;
            _inter = inter;
        }

        [Command("queue")]
        [Alias("q")]
        [RequireSongs]
        [Summary("Gets the song queue for the current guild.")]
        public async Task QueueAsync()
        {
            var guild = Context.Guild;
            var queue = _audio.GetQueue(guild.Id);

            var chunks = queue.ChunkBy(10);

            var time =  _audio.GetRawPosition(guild.Id).TotalSeconds + _audio.GetRawQueue(guild.Id).Sum(x => x.Length.TotalSeconds);

            var pages = new List<PageBuilder>();

            for (int i = 0; i < chunks.Count(); i++)
            {
                var chunk = chunks.ElementAt(i);
                pages.Add(new PageBuilder()
                    .WithColor(_rand.RandomColor())
                    .WithFields(new EmbedFieldBuilder()
                        .WithName($"#{i * 10 + 1}{(chunk.Count() == 1 ? "" : $" to #{i * 10 + chunk.Count()}")}")
                        .WithValue(string.Join('\n', chunk) + $"\n\n**Time left: {TimeSpan.FromSeconds(time).ToString(@"hh\:mm\:ss")} | Looping: {(_audio.IsLooping(guild.Id) ? "👍" : "👎")}**")
                    )
                    .WithTitle($"Total songs: {queue.Count} ")
                );
            }
            
            await _inter.SendPaginatorAsync(new StaticPaginatorBuilder()
                .WithUsers(Context.User)
                .WithDefaultEmotes()
                .WithFooter(PaginatorFooter.PageNumber)
                .WithPages(pages)
                .WithDeletion(DeletionOptions.AfterCapturedContext)
                .Build(), Context.Channel);
        }
    }
}