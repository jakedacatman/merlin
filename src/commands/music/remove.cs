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
        [RequireDjRole, RequireSongs]
        [Summary("Removes the song at the specified index.")]
        public async Task RemoveAsync(int index)
        {
            var id = Context.Guild.Id;

            if (index == 1)
            {
                await ReplyAsync("Can't remove the currently-playing song. Use the skip command to skip songs!");
                return;
            }
            
            await ReplyAsync(_audio.RemoveAt(id, index - 1) ? $"Removed the song at index {index}." : "Failed to remove the song; are you sure that there is a song at that position?");
        }
    }
}