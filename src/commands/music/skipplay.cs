using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using donniebot.classes;
using Interactivity;

namespace donniebot.commands
{
    [Name("Music")]
    public class SkipPlayCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;

        public SkipPlayCommand(AudioService audio, MiscService misc, CommandService cmds, GuildPrefix defPre) => _audio = audio;

        [Command("skipplay")]
        [Alias("sp", "skp", "skpl")]
        [RequireDjRole, RequireSongs, RequireSameVoiceChannel]
        [Summary("Skips the current song to play another.")]
        public async Task SkipPlayAsync([Summary("The URL or YouTube search query."), Remainder] string queryOrUrl = null)
        {
            if (_audio.IsLooping(Context.Guild.Id))
                _audio.ToggleLoop(Context.Guild.Id);

            await _audio.SkipAsync(Context.User as SocketGuildUser);
            await _audio.AddAsync(Context.User as SocketGuildUser, Context.Channel as SocketTextChannel, queryOrUrl, position: 0);
        }
    }
}