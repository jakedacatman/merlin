using System;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using Interactivity;

namespace donniebot.commands
{
    [Name("Music")]
    public class JoinCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;

        public JoinCommand(AudioService audio, MiscService misc)
        {
            _audio = audio;
            _misc = misc;
        }

        [Command("join")]
        [Alias("jo")]
        [Summary("Joins the current voice channel.")]
        public async Task JoinAsync()
        {
            var vc = (Context.User as SocketGuildUser).VoiceChannel;
            if (vc == null)
            {
                await ReplyAsync("You must be in a voice channel.");
                return;
            }

            await _audio.ConnectAsync(Context.Channel as SocketTextChannel, vc);
        }
    }
}