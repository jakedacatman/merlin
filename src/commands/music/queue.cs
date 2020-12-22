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
    [Name("Audio")]
    public class QueueCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;
        private readonly RandomService _rand;
        private readonly InteractivityService _inter;

        public QueueCommand(AudioService audio, MiscService misc, RandomService rand, InteractivityService inter)
        {
            _audio = audio;
            _misc = misc;
            _rand = rand;
            _inter = inter;
        }

        [Command("queue")]
        [Alias("q")]
        [Summary("Gets the song queue for the current guild.")]
        public async Task QueueCmd()
        {
            try
            {
                var guild = Context.Guild;
                var queue = _audio.GetQueue(guild.Id);

                if (!queue.Any())
                {
                    await ReplyAsync("There are no songs in the queue. Try adding some with `don.add`!");
                    return;
                }

                var chunks = queue.ChunkBy(10);

                var pages = new List<PageBuilder>();

                for (int i = 0; i < chunks.Count(); i++)
                {
                    var chunk = chunks.ElementAt(i);
                    pages.Add(new PageBuilder().WithColor(_rand.RandomColor()).WithFields(new EmbedFieldBuilder().WithName($"#{i * 10 + 1} to #{i * 10 + chunk.Count()}").WithValue(string.Join('\n', chunk))));
                }
                
                await _inter.SendPaginatorAsync(new StaticPaginatorBuilder()
                    .WithUsers(Context.User)
                    .WithDefaultEmotes()
                    .WithFooter(PaginatorFooter.PageNumber)
                    .WithPages(pages)
                    .Build(), Context.Channel);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}