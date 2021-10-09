﻿using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;

namespace donniebot.commands
{
    [Name("Misc")]
    public class EvalCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly DbService _db;

        public EvalCommand(DiscordShardedClient client, MiscService misc, DbService db)
        {
            _client = client;
            _misc = misc;
            _db = db;
        }

        [Command("eval")]
        [Alias("e", "evaluate")]
        [Summary("Evaluates C# code.")]
        [RequireOwner]
        public async Task EvalAsync([Summary("The code to evaluate."), Remainder] string code)
        {
            var msg = await _misc.EvaluateAsync(Context, code.Replace("--noreply", ""));
            if (!code.Contains("--noreply"))
                await ReplyAsync(embed: msg.Build());
        }
    }
}