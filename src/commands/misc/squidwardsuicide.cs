using System;
using System.Threading.Tasks;
using Discord.Commands;
using Interactivity;
using System.Collections.Generic;
using donniebot.services;
using Discord.WebSocket;

namespace donniebot.commands
{
    [Name("Misc")]
    public class SquidwardSuicideCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;

        public SquidwardSuicideCommand(MiscService misc)
        {
            _misc = misc;
        }

        [Command("squidwardsuicide")]
        [Alias("ss")]
        [Summary("Replaces the last two words of the previous message with \"squidward suicide\".")]
        public async Task SquidwardSuicideCmd()
        {
            try
            {
                var lastMsg = (await _misc.GetPreviousMessageAsync(Context.Channel as SocketTextChannel)).Content;
                var split = new List<string>(lastMsg.Split(' '));
                string msg;
                var ct = split.Count;
                if (ct < 2) msg = "squidward suicide";
                else 
                {
                    split[ct - 2] = "squidward";
                    split[ct - 1] = "suicide";

                    msg = string.Join(' ', split);
                }

                await ReplyAsync(msg);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}