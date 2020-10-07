﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Addons.Interactive;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Help")]
    public class CommandsCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly CommandService _commands;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public CommandsCommand(CommandService commands, MiscService misc, RandomService rand)
        {
            _commands = commands;
            _misc = misc;
            _rand = rand;
        }

        [Command("commands")]
        [Alias("cmds")]
        [Summary("Sends a list of bot commands.")]
        public async Task CommandsCmd([Summary("The category to view the commands of.")] string category = null)
        {
            try
            {
                Dictionary<string, List<string>> modules = new Dictionary<string, List<string>>();

                foreach (ModuleInfo module in _commands.Modules)
                {
                    if (module.Name == "") continue;
                    List<string> names = new List<string>();
                    if (!modules.ContainsKey(module.Name))
                        modules.Add(module.Name, names);
                    else
                        names = modules[module.Name];
                    foreach (CommandInfo cmd in module.Commands)
                    {
                        if (cmd.Summary == null) continue;
                        if (!names.Contains($"{cmd.Module.Group} {cmd.Name}")) names.Add($"{cmd.Module.Group} {cmd.Name}");
                    }
                }

                if (string.IsNullOrEmpty(category))
                {
                    var pages = new List<string>();
                    foreach (var module in modules)
                        pages.Add($"**{module.Key}**\n{string.Join(", ", module.Value)}");

                    await PagedReplyAsync(new PaginatedMessage 
                    { 
                        Pages = pages, 
                        Color = _rand.RandomColor(), 
                        Title = "Commands", 
                        AlternateDescription = "A list of commands" 
                    });
                }
                else
                {
                    var module = modules.Where(x => x.Key.ToLower() == category.ToLower()).First();
                    var fields = new List<EmbedFieldBuilder> { new EmbedFieldBuilder().WithIsInline(true).WithName(module.Key).WithValue($"**{string.Join(", ", module.Value)}**") };

                    var embed = new EmbedBuilder()
                        .WithColor(_rand.RandomColor())
                        .WithTitle("Commands")
                        .WithFields(fields)
                        .WithCurrentTimestamp();

                    await ReplyAsync(embed: embed.Build());
                }
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}
