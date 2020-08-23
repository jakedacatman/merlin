﻿﻿using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Misc")]
    public class EvalCommand : InteractiveBase<ShardedCommandContext>
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
        public async Task EvalCmd([Summary("The code to evaluate."), Remainder] string code)
        {
            try
            {
                if (code.Contains("--noreply"))
                    await _misc.EvaluateAsync(Context, code.Replace("--noreply", ""));
                else
                    await ReplyAsync(embed: (await _misc.EvaluateAsync(Context, code)).Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}