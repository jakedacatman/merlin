using System;
using Discord.Commands;
using donniebot.services;
using System.Threading.Tasks;
using donniebot.classes;
using Discord.WebSocket;
using System.Linq;

namespace donniebot.commands
{
    [Name("Music")]
    public class DisconnectCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;

        public DisconnectCommand(AudioService audio) => _audio = audio;

        [Command("disconnect")]
        [Alias("di", "dis", "leave")]
        [RequireDjRole]
        [Summary("Leaves the current voice channel.")]
        public async Task LeaveAsync()
        {
            var vc = Context.Guild.CurrentUser.VoiceChannel;
            if (vc == null)
            {
                await ReplyAsync("I am not connected to a voice channel.");
                return;
            }

            if ((Context.User as SocketGuildUser).VoiceChannel == vc)
                await _audio.DisconnectAsync(vc, Context.Channel);
            else
            {
                if (!_audio.GetListeningUsers(Context.Guild.Id).Any())  
                    await _audio.DisconnectAsync(vc, Context.Channel);
                else
                    await ReplyAsync("You are not in my voice channel, or there are people still listening.");
            }
        }
    }
}