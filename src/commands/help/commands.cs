﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using merlin.services;

namespace merlin.commands
{
    [Name("Help")]
    public class CommandsCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly CommandService _commands;
        private readonly MiscService _misc;
        private readonly RandomService _rand;
        private readonly InteractiveService _inter;

        public CommandsCommand(CommandService commands, MiscService misc, RandomService rand, InteractiveService inter)
        {
            _commands = commands;
            _misc = misc;
            _rand = rand;
            _inter = inter;
        }

        [Command("commands")]
        [Alias("cmds")]
        [Summary("Sends a list of bot commands.")]
        public async Task CommandsAsync([Summary("The category to view the commands of.")] string category = null)
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
                    if (!names.Contains($"{cmd.Module.Group} {cmd.Name}".TrimEnd(' '))) names.Add($"{cmd.Module.Group} {cmd.Name}".TrimEnd(' '));
                }
            }

            if (string.IsNullOrEmpty(category))
            {
                var pages = new List<PageBuilder>();
                foreach (var module in modules)
                    pages.Add(new PageBuilder()
                        .WithTitle("Commands")
                        .WithFields(new EmbedFieldBuilder().WithName(module.Key).WithValue(string.Join(", ", module.Value)))
                        .WithColor(_rand.RandomColor()));

                var msg = await _misc.SendPaginatorAsync(Context.User, pages, Context.Channel);

                if (msg.IsCanceled) await msg.Message.DeleteAsync();
            }
            else
            {
                KeyValuePair<string, List<string>> module;
                var query = modules.Where(x => x.Key.ToLower() == category.ToLower());
                if (query.Any()) 
                    module = query.First();
                else
                {
                    await ReplyAsync("Category not found.");
                    return;
                }

                var fields = new List<EmbedFieldBuilder> { new EmbedFieldBuilder().WithIsInline(true).WithName(module.Key).WithValue(string.Join(", ", module.Value)) };

                var embed = new EmbedBuilder()
                    .WithColor(_rand.RandomColor())
                    .WithTitle("Commands")
                    .WithFields(fields)
                    .WithCurrentTimestamp();

                await ReplyAsync(embed: embed.Build());
            }
        }
    }
}
