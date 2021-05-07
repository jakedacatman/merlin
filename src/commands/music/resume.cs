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
    public class ResumeCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;

        public ResumeCommand(AudioService audio, MiscService misc)
        {
            _audio = audio;
            _misc = misc;
        }

        [Command("resume")]
        [Alias("re", "res")]
        [RequireDjRole]
        [Summary("Resumes playback.")]
        public async Task ResumeAsync()
        {
            var vc = (Context.User as SocketGuildUser).VoiceChannel;
            if (vc == null)
            {
                await ReplyAsync("You must be in a voice channel.");
                return;
            }

            await _audio.ResumeAsync(Context.Guild.Id);
        }
    }
}