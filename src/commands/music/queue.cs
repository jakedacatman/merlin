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
        private readonly MiscService _misc;
        private readonly RandomService _rand;
        private readonly InteractivityService _inter;
        private readonly GuildPrefix _defPre;

        public QueueCommand(AudioService audio, MiscService misc, RandomService rand, InteractivityService inter, GuildPrefix defPre)
        {
            _audio = audio;
            _misc = misc;
            _rand = rand;
            _inter = inter;
            _defPre = defPre;
        }

        [Command("queue")]
        [Alias("q")]
        [Summary("Gets the song queue for the current guild.")]
        public async Task QueueAsync()
        {
            var guild = Context.Guild;
            var queue = _audio.GetQueue(guild.Id);

            if (!queue.Any())
            {
                await ReplyAsync($"There are no songs in the queue. Try adding some with `{_defPre.Prefix}add`!");
                return;
            }

            var chunks = queue.ChunkBy(10);

            var time = _audio.GetRawPosition(guild.Id).TotalSeconds + _audio.GetRawQueue(guild.Id).Sum(x => x.Length.TotalSeconds);

            var pages = new List<PageBuilder>();

            for (int i = 0; i < chunks.Count(); i++)
            {
                var chunk = chunks.ElementAt(i);
                pages.Add(new PageBuilder()
                    .WithColor(_rand.RandomColor())
                    .WithFields(new EmbedFieldBuilder()
                        .WithName($"#{i * 10 + 1} to #{i * 10 + chunk.Count()}")
                        .WithValue(string.Join('\n', chunk))
                    )
                    .WithTitle($"Total time left: {TimeSpan.FromSeconds(time).ToString(@"hh\:mm\:ss")}")
                );
            }
            
            await _inter.SendPaginatorAsync(new StaticPaginatorBuilder()
                .WithUsers(Context.User)
                .WithDefaultEmotes()
                .WithFooter(PaginatorFooter.PageNumber)
                .WithPages(pages)
                .Build(), Context.Channel);
        }
    }
}