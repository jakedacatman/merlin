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
    [Name("Audio")]
    public class PlayCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public PlayCommand(AudioService audio, MiscService misc, RandomService rand)
        {
            _audio = audio;
            _misc = misc;
            _rand = rand;
        }

        [Command("add")]
        [Alias("p", "play", "pl")]
        [Summary("Adds a song or playlist to the queue in order to be played.")]
        public async Task PlayCmd([Summary("The URL or YouTube search query."), Remainder] string queryOrUrl = null)
        {
            try
            {
                await _audio.AddAsync(Context.User as SocketGuildUser, Context.Channel as SocketTextChannel, queryOrUrl);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}