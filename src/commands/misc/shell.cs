using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Interactivity;
using System.Diagnostics;
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
        public async Task ShellCmd([Summary("The command to run"), Remainder]string command)
        {
            try
            {
                var output = await Shell.Run(command);
                if (string.IsNullOrWhiteSpace(output)) output = " ";
                await ReplyAsync($"```{output}```");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}