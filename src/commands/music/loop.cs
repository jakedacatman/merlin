using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.classes;
using donniebot.services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Interactivity;

namespace donniebot.commands
{
    [Name("Music")]
    public class LoopCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;

        public LoopCommand(AudioService audio) => _audio = audio;

        [Command("loop")]
        [Alias("l", "lo")]
        [RequireDjRole, RequireSameVoiceChannel]
        [Summary("Toggles looping the currently-playing song.")]
        public async Task LoopAsync()
        {
            if (!_audio.IsConnected(Context.Guild.Id))
            {
                await ReplyAsync("I am not connected to a voice channel.");
                return;
            }

            await ReplyAsync(_audio.ToggleLoop(Context.Guild.Id) ? "Looping the queue." : "Not looping the queue.");
        }
    }
}