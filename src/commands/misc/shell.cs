using System;
using System.Threading.Tasks;
using Discord.Commands;
using donniebot.services;
using donniebot.classes;

namespace donniebot.commands
{
    [Name("Misc")]
    public class ShellCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public ShellCommand(MiscService misc, RandomService rand)
        {
            _misc = misc;
            _rand = rand;
        }

        [Command("shell")]
        [Summary("Runs a shell command.")]
        [RequireOwner]
        public async Task ShellAsync([Summary("The command to run"), Remainder]string command)
        {
            var output = await Shell.RunAsync(command);
            if (string.IsNullOrWhiteSpace(output)) output = " ";
            await ReplyAsync($"```{output}```");
        }
    }
}