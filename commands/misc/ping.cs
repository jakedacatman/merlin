using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.Interactive;
using System.Diagnostics;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Misc")]
    public class PingCommand : InteractiveBase<ShardedCommandContext>
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
        public async Task PingCmd()
        {
            try
            {
                var s = Stopwatch.StartNew();
                var msg = await ReplyAsync("getting ping...");
                s.Stop();

                var embed = new EmbedBuilder()
                    .WithColor(_rand.RandomColor())
                    .WithTitle("Ping")
                    .WithDescription($"{s.ElapsedTicks/1000000d} ms")
                    .WithCurrentTimestamp();

                await msg.ModifyAsync(x => 
                {
                    x.Embed = embed.Build();
                    x.Content = " ";
                });
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}