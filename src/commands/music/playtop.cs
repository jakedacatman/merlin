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
    public class PlayTopCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;

        public PlayTopCommand(AudioService audio) => _audio = audio;

        [Command("playtop")]
        [Alias("pt", "tp")]
        [RequireDjRole, RequireSameVoiceChannel]
        [Summary("Adds a song or playlist to the beginning of the queue.")]
        public async Task PlayTopAsync([Summary("The URL or YouTube search query."), Remainder] string queryOrUrl = null) => await _audio.AddAsync(Context.User as SocketGuildUser, Context.Channel as SocketTextChannel, queryOrUrl, position: 0);
    }
}