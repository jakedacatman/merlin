using System;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using donniebot.services;
using Discord.Addons.Interactive;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using donniebot.classes;

namespace donniebot
{
    class Program
    {
        private DiscordShardedClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public static Task Main() => new Program().Start();

        private readonly string prefix = "don.";

        public async Task Start()
        {
            _client = new DiscordShardedClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = false,
                ConnectionTimeout = int.MaxValue,
                TotalShards = 2,
                DefaultRetryMode = RetryMode.AlwaysRetry,
                MessageCacheSize = 1024,
                ExclusiveBulkDelete = true
            });
            _commands = new CommandService(new CommandServiceConfig
            {
                ThrowOnError = true,
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
                IgnoreExtraArgs = false
            });

            NekoEndpoints nekoEndpoints;
            using (var hc = new HttpClient())
                nekoEndpoints = new NekoEndpoints(JsonConvert.DeserializeObject<JObject>(await hc.GetStringAsync("https://raw.githubusercontent.com/Nekos-life/nekos-dot-life/master/endpoints.json")));
            
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton(new Random())
                .AddSingleton(new LiteDatabase("database.db"))
                .AddSingleton<DbService>()
                .AddSingleton<MiscService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<ImageService>()
                .AddSingleton<ModerationService>()
                .AddSingleton(nekoEndpoints)
                .AddSingleton<NetService>()
                .AddSingleton<RandomService>()
                .AddSingleton<AudioService>()
                .BuildServiceProvider();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            var db = _services.GetService<DbService>();
            var apiKey = db.GetApiKey("discord");
            if (apiKey == null)
            {
                Console.WriteLine("What is the bot's token? (only logged to database.db)");
                apiKey = Console.ReadLine();
                db.AddApiKey("discord", apiKey);
            }

            await _client.LoginAsync(TokenType.Bot, apiKey);
            await _client.StartAsync();

            await _client.SetActivityAsync(new Game($"myself start up {_client.Shards.Count} shards", ActivityType.Watching));

            _client.Log += Log;
            _client.MessageReceived += MsgReceived;

            int counter = 1;

            _client.ShardConnected += async (DiscordSocketClient client) =>
            {
                if (counter >= _client.Shards.Count)
                {
                    try
                    {
                        await UpdateStatus(counter);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        await UpdateStatus(counter);
                    }
                }
                else
                {   
                    counter++;
                    return;
                }
            };

            _commands.Log += Log;

            if (!File.Exists("nsfw.txt"))
                await File.WriteAllTextAsync("nsfw.txt", await _services.GetService<NetService>().DownloadAsStringAsync("https://paste.jakedacatman.me/raw/YU4vA"));

            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
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

        private async Task MsgReceived(SocketMessage _msg)
        {
            try
            {
                if (!(_msg is SocketUserMessage msg) || _msg == null || string.IsNullOrEmpty(msg.Content)) return;
                ShardedCommandContext context = new ShardedCommandContext(_client, msg);
                if (context.User.IsBot) return;

                int mentPos = 0;
                if (msg.HasMentionPrefix(_client.CurrentUser, ref mentPos))
                {
                    var parseResult = ParseResult.FromSuccess(new List<TypeReaderValue> { new TypeReaderValue(msg.Content.Substring(mentPos), 1f) }, new List<TypeReaderValue>() );

                    await _commands.Commands.Where(x => x.Name == "" && x.Module.Group == "tag").First().ExecuteAsync(context, parseResult, _services);
                    return;
                }
                else if (msg.Content == _client.CurrentUser.Mention)
                {
                    var parseResult = ParseResult.FromSuccess(new List<TypeReaderValue> { new TypeReaderValue(_client.CurrentUser.Mention, 1f) }, new List<TypeReaderValue>() );
                    await _commands.Commands.Where(x => x.Name == "" && x.Module.Group == "tag").First().ExecuteAsync(context, parseResult, _services);
                    return;
                }

                int argPos = prefix.Length - 1;
                if (!msg.HasStringPrefix(prefix, ref argPos)) return;

                await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
            }   
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task UpdateStatus(int counter) => await _client.SetActivityAsync(new Game($"over {counter} out of {_client.Shards.Count} shards", ActivityType.Watching));
    }
}