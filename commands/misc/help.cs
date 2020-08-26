﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Help")]
    public class HelpCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly CommandService _commands;
        private readonly MiscService _misc;
        private readonly IServiceProvider _services;

        public HelpCommand(CommandService commands, MiscService misc, IServiceProvider services)
        {
            _commands = commands;
            _misc = misc;
            _services = services;
        }

        [Command("help")]
        [Alias("h")]
        [Summary("Brings up information about a specific command.")]
        public async Task HelpCmd([Summary("The command to get information about."), Remainder] string command = null)
        {
            try
            {
                if (command == null)
                {
                    await _commands.Commands.Where(x => x.Name == "commands").First().ExecuteAsync(Context, ParseResult.FromSuccess(new List<TypeReaderResult>(), new List<TypeReaderResult>()), _services);
                    return;
                }
                
                var cmds = _commands.Commands.Where(x => ((string.IsNullOrEmpty(x.Module.Group) ? "" : $"{x.Module.Group} ") + x.Name).TrimEnd(' ') == command);
                
                if (cmds.Any())
                    await ReplyAsync(embed: _misc.GenerateCommandInfo(cmds).Build());
                else 
                {
                    var aliases =  _commands.Commands.Where(x => x.Aliases.Any(y => ((string.IsNullOrEmpty(x.Module.Group) ? "" : $"{x.Module.Group} ") + y).TrimEnd(' ') == command));
                    
                    if (aliases.Any())
                        await ReplyAsync(embed: _misc.GenerateCommandInfo(aliases).Build());
                    else
                        await ReplyAsync("This command does not exist.");
                }
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}