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
using donniebot.classes;
using System.Linq;

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
                .AddSingleton<NetService>()
                .AddSingleton<RandomService>()
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

            _client.ShardConnected += (DiscordSocketClient client) =>
            {
                if (counter >= _client.Shards.Count)
                {
                    try
                    {
                        UpdateStatus(counter);
                        return Task.CompletedTask;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        UpdateStatus(counter);
                        return Task.CompletedTask;
                    }
                }
                else
                {   
                    counter++;
                    return Task.CompletedTask;
                }
            };

            _commands.Log += Log;

            if (!File.Exists("nsfw.txt"))
                await File.WriteAllTextAsync("nsfw.txt", await _services.GetService<NetService>().DownloadAsStringAsync("https://paste.jakedacatman.me/YU4vA"));

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

                int argPos = prefix.Length - 1;
                if (!msg.HasStringPrefix(prefix, ref argPos)) return;

                if (context.User.IsBot) return;
                await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
            }   
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void UpdateStatus(int counter)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await _client.SetActivityAsync(new Game($"over {counter} out of {_client.Shards.Count} shards", ActivityType.Watching));
                    await Task.Delay(5000);
                    await _client.SetActivityAsync(new Game($"don.help", ActivityType.Playing));
                    await Task.Delay(5000);
                }
            });
        }
    }
}