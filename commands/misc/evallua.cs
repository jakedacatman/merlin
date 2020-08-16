using System;
using System.Collections.Generic;   
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Misc")]
    public class EvalLuaCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly MiscService _misc;
        private readonly DbService _db;

        public EvalLuaCommand(DiscordShardedClient client, MiscService misc, DbService db)
        {
            _client = client;
            _misc = misc;
            _db = db;
        }

        [Command("evallua")]
        [Alias("el", "elua", "evaluatelua")]
        [Summary("Evaluates Lua code.")]
        //[RequireOwner]
        public async Task EvalLuaCmd([Summary("The code to evaluate."), Remainder] string code)
        {
            try
            {
                await ReplyAsync(embed: (await _misc.EvaluateLuaAsync(Context, code)).Build());
            }
            catch (System.Net.WebException e) when (e.Message == "The remote server returned an error: (400) Bad Request.")
            {
                await ReplyAsync("The server returned an HTTP 400 error (bad request). Are you doing something shady? :thinking:");
            }
            catch (System.Net.WebException e) when (e.Message == "The remote server returned an error: (413) Payload Too Large.")
            {
                await ReplyAsync("The server returned an HTTP 413 error (payload too large). Are you doing something shady? :thinking:");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}