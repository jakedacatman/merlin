using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using donniebot.classes;
using System.Threading.Tasks;
using System.Collections.Generic;
using Interactivity;
using LiteDB;
using System.Linq;

namespace donniebot.commands
{
    [Name("Music")]
    public class PlayShuffleCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;

        public PlayShuffleCommand(AudioService audio) => _audio = audio;

        [Command("playshuffle")]
        [Alias("ps", "playsh", "plsh")]
        [Summary("Adds a song or playlist to the queue, then shuffles the queue.")]
        [RequireDjRole]
        public async Task PlayShuffleAsync([Summary("The URL or YouTube search query."), Remainder] string queryOrUrl = null) => await _audio.AddAsync(Context.User as SocketGuildUser, Context.Channel as SocketTextChannel, queryOrUrl, true);
    }
}