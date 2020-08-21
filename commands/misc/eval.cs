﻿using System;
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
                await ReplyAsync(embed: (await _misc.EvaluateAsync(Context, code)).Build());
            }
            catch (System.Net.WebException e) when (e.Message == "The remote server returned an error: (400) Bad Request.")
            {
                await ReplyAsync("The pastebin returned an HTTP 400 error (bad request). Are you doing something shady? :thinking:");
            }
            catch (System.Net.WebException e) when (e.Message == "The remote server returned an error: (413) Payload Too Large.")
            {
                await ReplyAsync("The pastebin returned an HTTP 413 error (payload too large). Are you doing something shady? :thinking:");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}