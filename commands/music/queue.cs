using System;
using Discord;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Audio")]
    public class QueueCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public QueueCommand(AudioService audio, MiscService misc, RandomService rand)
        {
            _audio = audio;
            _misc = misc;
            _rand = rand;
        }

        [Command("queue")]
        [Alias("q")]
        [Summary("Gets the song queue for the current guild.")]
        public async Task QueueCmd()
        {
            try
            {
                var guild = Context.Guild;
                var queue = _audio.GetQueue(guild.Id);

                if (!queue.Any())
                {
                    await ReplyAsync("There are no songs in the queue. Try adding some with `don.add`!");
                }
                else
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(_rand.RandomColor())
                        .WithTitle($"Queue for `{guild.Name}`")
                        .WithThumbnailUrl(guild.IconUrl)
                        .WithDescription(string.Join("\n", queue))
                        .WithCurrentTimestamp()
                        .Build());
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}