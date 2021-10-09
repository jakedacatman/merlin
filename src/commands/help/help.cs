﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Interactivity;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Help")]
    public class HelpCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly CommandService _commands;
        private readonly MiscService _misc;
        private readonly IServiceProvider _services;
        private readonly InteractivityService _inter;

        public HelpCommand(CommandService commands, MiscService misc, IServiceProvider services, InteractivityService inter)
        {
            _commands = commands;
            _misc = misc;
            _services = services;
            _inter = inter;
        }

        [Command("help")]
        [Alias("h")]
        [Summary("Brings up information about a specific command.")]
        public async Task HelpAsync([Summary("The command to get information about."), Remainder] string command = null)
        {
            if (command == null) //run don.cmds
            {
                await _commands.Commands.Where(x => x.Name == "commands").First().ExecuteAsync(Context, ParseResult.FromSuccess(new List<TypeReaderValue> { new TypeReaderValue(null, 1f) }, new List<TypeReaderValue>()), _services);
                return;
            }
            
            var cmds = _commands.Commands.Where(x => ((string.IsNullOrEmpty(x.Module.Group) ? "" : $"{x.Module.Group} ") + x.Name).TrimEnd(' ') == command);
            
            if (cmds.Any())
                await _inter.SendPaginatorAsync(_misc.GenerateCommandInfo(cmds, Context.User as SocketGuildUser).Build(), Context.Channel);
            else 
            {
                var split = command.Split(' ');

                var aliases =  _commands.Commands.Where(x => ((x.Module.Group == null && split.Length < 2) || 
                    x.Module.Group == split[0]) && 
                    x.Aliases.Any(y => y == command || (split.Length > 1 && y == split[1]
                )));
                
                if (aliases.Any())
                    await _inter.SendPaginatorAsync(_misc.GenerateCommandInfo(aliases, Context.User as SocketGuildUser).Build(), Context.Channel);
                else
                    await ReplyAsync("This command does not exist.");
            }
        }
    }
}