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

        private readonly Dictionary<Type, string> typeAliases = new Dictionary<Type, string>()
        {
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(void), "void" },
            { typeof(SocketGuildUser), "user" }
        };

        private readonly Dictionary<Type, string> preconditionAliases = new Dictionary<Type, string>()
        {
            { typeof(RequireOwnerAttribute), "owner-only" },
            { typeof(RequireUserPermissionAttribute), "user requires perms" },
            { typeof(RequireNsfwAttribute), "requires nsfw channel" },
            { typeof(RequireBotPermissionAttribute), "bot requires perms" },
            { typeof(RequireContextAttribute), "must be invoked in a guild or dm" }
        };

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
                var aliases =  _commands.Commands.Where(x => x.Aliases.Any(y => (string.IsNullOrEmpty(x.Module.Group) ? "" : $"{x.Module.Group} " + y).TrimEnd(' ') == command));
                if (cmds.Any())
                {
                    var firstCmd = cmds.First();
                    string preconditions = null;
                    if (firstCmd.Preconditions.Any())
                        foreach (PreconditionAttribute p in firstCmd.Preconditions)
                        {
                            var txt = "no info";
                            switch (p.GetType().ToString())
                            {
                                case "Discord.Commands.RequireUserPermissionAttribute":
                                    var attr = (RequireUserPermissionAttribute)p;
                                    txt = attr.GuildPermission.HasValue ? attr.GuildPermission.Value.ToString() : txt;
                                    break;

                                case "Discord.Commands.RequireBotPermissionAttribute":
                                    var attr2 = (RequireBotPermissionAttribute)p;
                                    txt = attr2.GuildPermission.HasValue ? attr2.GuildPermission.Value.ToString() : txt;
                                    break;
                            }
                            preconditions += $"{preconditionAliases[p.GetType()]} ({txt})\n";
                        }

                    var name = ((string.IsNullOrEmpty(firstCmd.Module.Group) ? "" : $"{firstCmd.Module.Group} ") + firstCmd.Name).TrimEnd(' ');

                    var fields = new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder().WithName("Name").WithValue(name).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Category").WithValue(firstCmd.Module.Name ?? "(none)").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Aliases").WithValue(firstCmd.Aliases.Count > 1 ? string.Join(", ", firstCmd.Aliases.Where(x => x != firstCmd.Name)) : "(none)").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Summary").WithValue(firstCmd.Summary ?? "(none)").WithIsInline(false),
                        new EmbedFieldBuilder().WithName("Preconditions").WithValue(preconditions ?? "(none)").WithIsInline(false),
                        new EmbedFieldBuilder().WithName("Parameters").WithValue(" ").WithIsInline(false)
                    };
                    int counter = 1;
                    StringBuilder sb = new StringBuilder();
                    foreach (var cmd in cmds)
                    {
                        var parameters = new List<string>();
                        foreach (ParameterInfo param in cmd.Parameters)
                            parameters.Add($"{param} ({typeAliases[param.Type]}{(param.DefaultValue != null ? ", default = " + param.DefaultValue.ToString() : "")}): {param.Summary}");
                        
                        sb.Append($"**{counter}.**\n " + (parameters.Any() ? string.Join("\n", parameters) : "(none)") + "\n\n");
                        counter++;
                    }

                    var last = fields.Last();
                    fields.Remove(last);
                    last.WithValue(sb.ToString());
                    fields.Add(last);

                    EmbedBuilder embed = new EmbedBuilder()
                        .WithTitle($"Information for {name}:")
                        .WithColor(_misc.RandomColor())
                        .WithCurrentTimestamp()
                        .WithFields(fields);

                    await ReplyAsync(embed: embed.Build());
                }
                else if (aliases.Any())
                {
                    var firstAlias = aliases.First();
                    string preconditions = null;
                    if (firstAlias.Preconditions.Any())
                        foreach (PreconditionAttribute p in firstAlias.Preconditions)
                        {
                            var txt = "no info";
                            switch (p.GetType().ToString())
                            {
                                case "Discord.Commands.RequireUserPermissionAttribute":
                                    var attr = (RequireUserPermissionAttribute)p;
                                    txt = attr.GuildPermission.HasValue ? attr.GuildPermission.Value.ToString() : txt;
                                    break;

                                case "Discord.Commands.RequireBotPermissionAttribute":
                                    var attr2 = (RequireBotPermissionAttribute)p;
                                    txt = attr2.GuildPermission.HasValue ? attr2.GuildPermission.Value.ToString() : txt;
                                    break;
                            }
                            preconditions += $"{preconditionAliases[p.GetType()]} ({txt})\n";
                        }

                    var name = ((string.IsNullOrEmpty(firstAlias.Module.Group) ? "" : $"{firstAlias.Module.Group} ") + firstAlias.Name).TrimEnd(' ');
                    var fields = new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder().WithName("Name").WithValue(name).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Category").WithValue(firstAlias.Module.Name ?? "(none)").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Aliases").WithValue(firstAlias.Aliases.Count > 1 ? string.Join(", ", firstAlias.Aliases.Where(x => x != firstAlias.Name)) : "(none)").WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Summary").WithValue(firstAlias.Summary ?? "(none)").WithIsInline(false),
                        new EmbedFieldBuilder().WithName("Preconditions").WithValue(preconditions ?? "(none)").WithIsInline(false),
                        new EmbedFieldBuilder().WithName("Parameters").WithValue(" ").WithIsInline(false)
                    };
                    int counter = 1;
                    StringBuilder sb = new StringBuilder();
                    foreach (var cmd in aliases)
                    {
                        var parameters = new List<string>();
                        foreach (ParameterInfo param in cmd.Parameters)
                            parameters.Add($"{param} ({typeAliases[param.Type]}{(param.DefaultValue != null ? ", default = " + param.DefaultValue.ToString() : "")}): {param.Summary}");

                        sb.Append($"**{counter}.**\n " + (parameters.Any() ? string.Join("\n", parameters) : "(none)") + "\n\n");
                        counter++;
                    }

                    var last = fields.Last();
                    fields.Remove(last);
                    last.WithValue(sb.ToString());
                    fields.Add(last);

                    EmbedBuilder embed = new EmbedBuilder()
                        .WithTitle($"Information for {firstAlias.Name}:")
                        .WithColor(_misc.RandomColor())
                        .WithCurrentTimestamp()
                        .WithFields(fields);

                    await ReplyAsync(embed: embed.Build());
                }
                else
                {
                    await ReplyAsync("This command does not exist.");
                    return;
                }
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}