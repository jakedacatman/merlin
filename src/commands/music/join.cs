using System;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using Interactivity;
using donniebot.classes;

namespace donniebot.commands
{
    [Name("Music")]
    public class JoinCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;

        public JoinCommand(AudioService audio) => _audio = audio;

        [Command("join")]
        [Alias("jo")]
        [Summary("Joins the current voice channel.")]
        [RequireVoiceChannel]
        public async Task JoinAsync() => await _audio.ConnectAsync(Context.Channel as SocketTextChannel, (Context.User as SocketGuildUser).VoiceChannel);
    }
}