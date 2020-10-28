using System;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Audio")]
    public class JoinCommand : InteractiveBase<ShardedCommandContext>
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
        public async Task JoinCmd()
        {
            try
            {
                var vc = (Context.User as SocketGuildUser).VoiceChannel;
                if (vc == null)
                {
                    await ReplyAsync("You must be in a voice channel.");
                    return;
                }

                await _audio.ConnectAsync(vc);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}