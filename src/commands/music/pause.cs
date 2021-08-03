using System;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using donniebot.classes;
using Interactivity;

namespace donniebot.commands
{
    [Name("Music")]
    public class PauseCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;

        public PauseCommand(AudioService audio) => _audio = audio;

        [Command("pause")]
        [Alias("pa", "pau")]
        [RequireDjRole, RequireVoiceChannel]
        [Summary("Pauses playback.")]
        public async Task PauseAsync() => await _audio.PauseAsync(Context.Guild.Id);
    }
}