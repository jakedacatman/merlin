using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Interactivity;
using System.Diagnostics;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Misc")]
    public class PingCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public PingCommand(MiscService misc, RandomService rand)
        {
            _misc = misc;
            _rand = rand;
        }

        [Command("ping")]
        [Alias("pi")]
        [Summary("Gets the bot's latency.")]
        public async Task PingAsync()
        {
            var s = Stopwatch.StartNew();
            var msg = await ReplyAsync("getting ping...");
            s.Stop();

            var embed = new EmbedBuilder()
                .WithColor(_rand.RandomColor())
                .WithTitle("Ping")
                .WithDescription($"{s.ElapsedTicks/1000000d} ms (calculated)\n{Context.Client.GetShardFor(Context.Guild).Latency} ms (estimated)")
                .WithCurrentTimestamp();

            await msg.ModifyAsync(x => 
            {
                x.Embed = embed.Build();
                x.Content = " ";
            });
        }
    }
}