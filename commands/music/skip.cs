using System;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Audio")]
    public class SkipCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;

        public SkipCommand(AudioService audio, MiscService misc)
        {
            _audio = audio;
            _misc = misc;
        }

        [Command("skip")]
        [Alias("sk")]
        [Summary("Votes to skip the current song.")]
        public async Task SkipCmd()
        {
            try
            {
                var skipCount = _audio.Skip(Context.User as SocketGuildUser);
                if (skipCount > 0)
                    await ReplyAsync($"Voted to skip! There are now {skipCount} votes.");
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}