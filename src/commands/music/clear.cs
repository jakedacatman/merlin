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
    public class ClearCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;

        public ClearCommand(AudioService audio) => _audio = audio;

        [Command("clear")]
        [Alias("cq")]
        [RequireDjRole, RequireSongs]
        [Summary("Clears the song queue.")]
        public async Task ClearAsync()
        {
            if (!_audio.IsConnected(Context.Guild.Id))
            {
                await ReplyAsync("I am not connected to a voice channel.");
                return;
            }

            await ReplyAsync(_audio.ClearQueue(Context.Guild.Id) ? "🗑️ Cleared the queue 💥" : "Failed to clear the queue.");
        }
    }
}