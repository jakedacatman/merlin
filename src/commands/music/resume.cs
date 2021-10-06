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

        public ResumeCommand(AudioService audio, MiscService misc) => _audio = audio;

        [Command("resume")]
        [Alias("re", "res")]
        [RequireDjRole, RequireSameVoiceChannel]
        [Summary("Resumes playback.")]
        public async Task ResumeAsync() => await _audio.ResumeAsync(Context.Guild.Id);
    }
}