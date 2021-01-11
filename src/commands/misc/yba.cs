using System;
using System.Threading.Tasks;
using Discord.Commands;
using Interactivity;
using System.Collections.Generic;
using donniebot.services;
using Discord.WebSocket;
using System.Linq;

namespace donniebot.commands
{
    [Name("Misc")]
    public class YbaCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;

        public YbaCommand(MiscService misc)
        {
            _misc = misc;
        }

        [Command("yba")]
        [Alias("y")]
        [Summary("Generates a copypasta based on the previous message.")]
        public async Task SquidwardSuicideCmd()
        {
            try
            {
                var lastMsg = (await _misc.GetPreviousMessageAsync(Context.Channel as SocketTextChannel)).Content;
                
                if (lastMsg.Length > 100)
                {
                    await ReplyAsync("Message too long.");
                    return;
                }
                
                var split = new List<string>(lastMsg.Split(' '));
                var ct = lastMsg.Length;
                var sentenceCount = lastMsg.Count(x => x == '.' || x == '?' || x == '!');
                if (sentenceCount < 1) sentenceCount = 1;
                
                string msg = lastMsg.Any() ? lastMsg : "ez";

                await ReplyAsync($"\"{msg}\" He had done it, through the typing of just {split.Count}" +
                    $" word{(split.Count > 1 ? "s" : "")}, {ct} glyph{(ct > 1 ? "s" : "")}, "+
                    $"{sentenceCount} sentence{(sentenceCount > 1 ? "s" : "")}, he had won the argument. His cheeto-dusted fingers were raised high in jubilation as his opponent had no relevant response. He bound to finally get laid, his 31 years of virginity coming to an end. He froze at the sound of a beep, it was his microwave. His taquitos were ready, and he lifted himself up from his chair leaving and orange stain on the black matte finish. An expensive chair his parents had bought him for being a good boy. He sat back down, opened an incognito tab and started to do his business to his 3000 JoJo waifus. Taking a bite of his taquito, the entirety of the insides were spilled onto his sad, erect weiner, leaving only a concoction of semen, salsa, and depression. All in the day of an YBA player.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}