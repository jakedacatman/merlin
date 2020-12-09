using System;
using Discord;
using System.Linq;
using Discord.Commands;
using donniebot.classes;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Audio")]
    public class CurrentCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public CurrentCommand(AudioService audio, MiscService misc, RandomService rand)
        {
            _audio = audio;
            _misc = misc;
            _rand = rand;
        }

        [Command("current")]
        [Alias("nowplaying", "np")]
        [Summary("Gets the currently-playing song, if any.")]
        public async Task QueueCmd()
        {
            try
            {
                var id = Context.Guild.Id;

                if (!_audio.HasSongs(id))
                {
                    await ReplyAsync("There are no songs in the queue. Try adding some with `don.add`!");
                    return;
                }

                var song = _audio.GetCurrent(id);
                if (song == null)
                {
                    await ReplyAsync("There is no song playing right now.");
                    return;
                }

                await ReplyAsync(embed: new EmbedBuilder()
                    .WithTitle("Now Playing")
                    .WithFields(new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder().WithName("Title").WithValue(song.Title).WithIsInline(false),
                        new EmbedFieldBuilder().WithName("Author").WithValue(song.Author).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Length").WithValue(song.Length.ToString("g")).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Size").WithValue(_misc.PrettyFormat(song.Size, 3)).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("Queuer").WithValue(Context.Guild.GetUser(song.QueuerId)).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("URL").WithValue(song.Url).WithIsInline(true)
                    })
                    .WithColor(_rand.RandomColor())
                    .WithThumbnailUrl(song.ThumbnailUrl)
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