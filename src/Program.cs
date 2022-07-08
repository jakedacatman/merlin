using System;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using merlin.services;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using merlin.classes;
using Fergun.Interactive;

namespace merlin
{
    class Program
    {
        private DiscordShardedClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public static Task Main() => new Program().StartAsync();

        private readonly string defaultPrefix = "mer.";

        private DbService _db;

        public async Task StartAsync()
        {
            _client = new DiscordShardedClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.All,
                ConnectionTimeout = int.MaxValue,
                DefaultRetryMode = RetryMode.AlwaysRetry,
                MessageCacheSize = 1024
            });

            _commands = new CommandService(new CommandServiceConfig
            {
                ThrowOnError = true,
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
                IgnoreExtraArgs = false
            });

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton(new Random())
                .AddSingleton(new LiteDatabase("database.db"))
                .AddSingleton<DbService>()
                .AddSingleton<MiscService>()
                .AddSingleton(new GuildPrefix { GuildId = 0, Prefix = defaultPrefix })
                .AddSingleton<ImageService>()
                .AddSingleton<ModerationService>()
                .AddSingleton<NetService>()
                .AddSingleton<RandomService>()
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _db = _services.GetService<DbService>();
                        
            Console.WriteLine($"Current directory: {Environment.CurrentDirectory}");
            var apiKey = _db.GetApiKey("discord");
            if (apiKey == null)
            {
                Console.WriteLine("What is the bot's token? (only logged to database.db)");
                apiKey = Console.ReadLine();
                _db.AddApiKey("discord", apiKey);
                Console.Clear();
            }

            await _client.LoginAsync(TokenType.Bot, apiKey);
            await _client.StartAsync();

            await _client.SetActivityAsync(new Game($"myself start up {_client.Shards.Count} shards", ActivityType.Watching));

            _client.Log += LogAsync;
            _client.MessageReceived += MsgReceivedAsync;

            _client.ShardConnected += async (DiscordSocketClient client) =>
            {
                try
                {
                    await UpdateStatusAsync(client);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await UpdateStatusAsync(client);
                }
            };

            _commands.Log += LogAsync;

            _commands.CommandExecuted += CommandExecutedAsync;

            if (!File.Exists("phrases.txt"))
                await File.WriteAllTextAsync("phrases.txt", await _services.GetService<NetService>().DownloadAsStringAsync("https://paste.jakedacatman.me/raw/8foSm"));

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage msg)
        {
            try
            {
                var toWrite = $"{DateTime.Now,19} [{msg.Severity,8}] {msg.Source}: {msg.Message ?? "no message"}";
                if (msg.Exception != null) toWrite += $" (exception: {msg.Exception})";
                Console.WriteLine(toWrite);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return Task.CompletedTask;
            }
        }

        private async Task MsgReceivedAsync(SocketMessage _msg)
        {
            try
            {
                if (!(_msg is SocketUserMessage msg) || _msg == null || string.IsNullOrEmpty(msg.Content)) return;
                ShardedCommandContext context = new ShardedCommandContext(_client, msg);
                if (context.User.IsBot) return;

                int mentPos = 0;

                var pre = _db.GetPrefix(context.Guild.Id)?.Prefix ?? defaultPrefix;
                
                if (msg.HasMentionPrefix(_client.CurrentUser, ref mentPos))
                {
                    var parseResult = ParseResult.FromSuccess(new List<TypeReaderValue> { new TypeReaderValue(msg.Content.Substring(mentPos), 1f) }, new List<TypeReaderValue>() );

                    await _commands.Commands.Where(x => x.Name == "" && x.Module.Group == "tag").First().ExecuteAsync(context, parseResult, _services);
                    return;
                }
                else if (msg?.Content == _client.CurrentUser.Mention)
                {
                    await context.Channel.SendMessageAsync($"My prefix is `{pre}`.");
                    return;
                }

                int argPos = pre.Length - 1;
                if (!msg.HasStringPrefix(pre, ref argPos))
                {
                    argPos = defaultPrefix.Length - 1;
                    if (!msg.HasStringPrefix(defaultPrefix, ref argPos))
                        return;
                }

                await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
            }   
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> info, ICommandContext context, IResult res)
        {
            if (!res.IsSuccess)
            {
                var em = new EmbedBuilder()
                    .WithColor(_services
                        .GetService<RandomService>()
                        .RandomColor()
                    )
                    .WithCurrentTimestamp()
                    .WithAuthor(x => 
                    {
                        x.Name = context.User.Username;
                        x.IconUrl = context.User.GetAvatarUrl();
                    })
                    .WithTitle("Command failed")
                    .WithDescription(res.ErrorReason);

                switch (res.Error)
                {
                    case CommandError.UnmetPrecondition:
                    {
                        em
                            .WithTitle("🛑 Command failed precondition check")
                            .WithDescription(res.ErrorReason);
                        break;
                    }
                    case CommandError.BadArgCount:
                    {
                        em.WithTitle("⁉️ Improper amount of arguments");

                        var value = info.IsSpecified ? info.Value : null;

                        if (value is null)
                            em.WithDescription(res.ErrorReason);
                        else
                        {
                            var pre = _services.GetService<DbService>().GetPrefix(context.Guild.Id)?.Prefix ?? defaultPrefix;

                            var cmd = context.Message.Content.Split(' ')[0].Replace(pre, "");

                            em.WithDescription($"Usage: `{pre}{cmd} {string.Join(' ', value.Parameters.Select(x => x.Name))}`");
                        }

                        break;
                    }
                    case CommandError.Exception:
                    {
                        em = await _services.GetService<MiscService>().GenerateErrorMessageAsync(((ExecuteResult)res).Exception);
                        break;
                    }
                    case CommandError.UnknownCommand:
                        return;
                }

                await context.Channel.SendMessageAsync(embed: em.Build(), messageReference: new MessageReference(context.Message.Id), allowedMentions: AllowedMentions.None);
            }
        }

        private async Task UpdateStatusAsync(DiscordSocketClient client) => await client.SetActivityAsync(new Game($"over shard {client.ShardId + 1}/{_client.Shards.Count}", ActivityType.Watching));
    }
}