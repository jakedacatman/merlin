using System;
using Discord;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using donniebot.classes;
using Interactivity;

namespace donniebot.commands
{
    [Name("Music")]
    public class ShuffleCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public ShuffleCommand(AudioService audio, MiscService misc, RandomService rand)
        {
            _audio = audio;
            _misc = misc;
            _rand = rand;
        }

        [Command("shuffle")]
        [Alias("sh")]
        [RequireDjRole]
        [Summary("Shuffles the song queue.")]
        public async Task ShuffleAsync()
        {
            try
            {
                var id = Context.Guild.Id;

                if (!_audio.HasSongs(id))
                {
                    await ReplyAsync("There are no songs in the queue.");
                    return;
                }

                _audio.Shuffle(id);
                
                await ReplyAsync("Shuffled the queue.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessageAsync(e)).Build());
            }
        }
    }
}