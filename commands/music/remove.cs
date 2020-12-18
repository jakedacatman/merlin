using System;
using Discord;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using Interactivity;

namespace donniebot.commands
{
    [Name("Audio")]
    public class RemoveCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public RemoveCommand(AudioService audio, MiscService misc, RandomService rand)
        {
            _audio = audio;
            _misc = misc;
            _rand = rand;
        }

        [Command("remove")]
        [Alias("rem")]
        [Summary("Removes the song at the specified index.")]
        public async Task ShuffleCmd(int index)
        {
            try
            {
                var id = Context.Guild.Id;

                if (!_audio.HasSongs(id))
                {
                    await ReplyAsync("There are no songs in the queue.");
                    return;
                }

                _audio.RemoveAt(id, index - 1);
                
                await ReplyAsync($"Removed the song at index {index}.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}