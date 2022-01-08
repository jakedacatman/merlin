using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Diagnostics;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Misc")]
    public class ChooseCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public ChooseCommand(MiscService misc, RandomService rand)
        {
            _misc = misc;
            _rand = rand;
        }

        [Command("choose")]
        [Alias("ch")]
        [Summary("Chooses between several options.")]
        public async Task ChooseAsync(params string[] options) => await ReplyAsync(options[_rand.RandomNumber(0, options.Length - 1)]);
    }
}