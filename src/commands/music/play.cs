using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Interactivity;

namespace donniebot.commands
{
    [Name("Music")]
    public class AddCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;

        public AddCommand(AudioService audio) => _audio = audio;

        [Command("add")]
        [Alias("p", "play", "pl")]
        [Summary("Adds a song or playlist to the queue in order to be played.")]
        public async Task AddAsync([Summary("The URL or YouTube search query."), Remainder] string queryOrUrl = null) => await _audio.AddAsync(Context.User as SocketGuildUser, Context.Channel as SocketTextChannel, queryOrUrl);
    }
}