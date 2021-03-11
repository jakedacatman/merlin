using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Interactivity;
using System.Collections.Generic;
using System.Linq;
using donniebot.services;
using Discord.WebSocket;
using Discord.Net;
using System.IO;

namespace donniebot.commands
{
    [Name("Misc")]
    public class VersionCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;

        public VersionCommand(MiscService misc)
        {
            _misc = misc;
        }

        [Command("version")]
        [Alias("ver", "v")]
        [Summary("Gets the bot's current version as determined by its latest git commit.")]
        public async Task VersionCmd()
        {
            try
            {
                if (!File.Exists(".version"))
                    await ReplyAsync("No version file found. If you are the bot owner, make sure that the .version file from the git repository is copied over to the same directory as tht bot executable.");
                else
                {
                    var file = File.OpenText(".version");
                    await file.ReadLineAsync();
                    var commit = (await file.ReadLineAsync()).Replace("commit ", "");
                    
                    await ReplyAsync($"My current version is `{commit.Substring(0, 7)}` (long commit: `{commit}`)");
                }
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}