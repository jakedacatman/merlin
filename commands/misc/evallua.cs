using System;
using System.Threading.Tasks;
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
        public async Task EvalLuaCmd([Summary("The code to evaluate."), Remainder] string code)
        {
            try
            {
                var channel = Context.Channel as SocketTextChannel;
                if (code.Contains("--noreply"))
                    await _misc.EvaluateLuaAsync(channel, code.Replace("--noreply", ""));
                else
                await ReplyAsync(embed: (await _misc.EvaluateLuaAsync(channel, code)).Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}