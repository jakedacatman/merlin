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

        public ShuffleCommand(AudioService audio, MiscService misc, RandomService rand) => _audio = audio;

        [Command("shuffle")]
        [Alias("sh")]
        [RequireDjRole, RequireSongs, RequireVoiceChannel, RequireSameVoiceChannel]
        [Summary("Shuffles the song queue.")]
        public async Task ShuffleAsync()
        {
            _audio.Shuffle(Context.Guild.Id);
            
            await ReplyAsync("🎲 Shuffled the queue!");
        }
    }
}