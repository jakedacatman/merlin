using System;
using merlin.services;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;

namespace merlin.classes
{
    public class Globals
    {
        public ShardedCommandContext _context { get; internal set; }
        public DiscordShardedClient _client { get; internal set; }
        public SocketGuildUser _user { get; internal set; }
        public SocketGuild _guild { get; internal set; }
        public ISocketMessageChannel _channel { get; internal set; }
        public SocketUserMessage _message { get; internal set; }
        public CommandService _commands { get; internal set; }
        public IServiceProvider _services { get; internal set; }
        public DbService _db { get; internal set; }
        public MiscService _misc { get; internal set; }
        public FakeConsole Console { get; internal set; }
        public Random Random { get; internal set; }
        public ImageService _img {get; internal set; }
        public RandomService _rand {get; internal set; }
        public NetService _net {get; internal set; }
        public Globals _globals { get; internal set; }
        public Process current { get; } = Process.GetCurrentProcess();
        public string[] Imports { get; internal set; } = new string[]
        {
            "System",
            "System.Collections",
            "System.Collections.Generic",
            "System.Linq",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Threading.Tasks",
            "Discord",
            "Discord.Commands",
            "Discord.WebSocket",
            "Discord.API",
            "Discord.Rest",
            "System.Diagnostics",
            "Microsoft.CodeAnalysis.CSharp.Scripting",
            "Microsoft.CodeAnalysis.Scripting",
            "System.Reflection",
            "merlin",
            "merlin.classes",
            "merlin.commands",
            "merlin.services",
            "Newtonsoft.Json",
            "Newtonsoft.Json.Linq",
            "System.Numerics",
            "LiteDB",
            "Microsoft.Extensions.DependencyInjection"
        };
    }
}