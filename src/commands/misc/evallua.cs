using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;

namespace donniebot.commands
{
    [Name("Misc")]
    public class EvalLuaCommand : ModuleBase<ShardedCommandContext>
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
        public async Task EvalLuaAsync([Summary("The code to evaluate."), Remainder] string code)
        {
            var channel = Context.Channel as SocketTextChannel;
            var msg = await _misc.EvaluateLuaAsync(channel, code.Replace("--noreply", ""));
            if (!code.Contains("--noreply"))
                await ReplyAsync(embed: msg.Build());
        }
    }
}