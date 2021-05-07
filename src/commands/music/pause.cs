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
        private readonly MiscService _misc;

        public PauseCommand(AudioService audio, MiscService misc)
        {
            _audio = audio;
            _misc = misc;
        }

        [Command("pause")]
        [Alias("pa", "pau")]
        [RequireDjRole]
        [Summary("Pauses playback.")]
        public async Task PauseAsync()
        {
            var vc = (Context.User as SocketGuildUser).VoiceChannel;
            if (vc == null)
            {
                await ReplyAsync("You must be in a voice channel.");
                return;
            }

            await _audio.PauseAsync(Context.Guild.Id);
        }
    }
}