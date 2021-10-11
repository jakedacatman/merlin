using System;
using Discord;
using System.Linq;
using Discord.Commands;
using donniebot.classes;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Interactivity;

namespace donniebot.commands
{
    [Name("Music")]
    public class SongInfoCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;
        private readonly RandomService _rand;
        private readonly GuildPrefix _defPre;

        public SongInfoCommand(AudioService audio, MiscService misc, RandomService rand, GuildPrefix defPre)
        {
            _audio = audio;
            _misc = misc;
            _rand = rand;
            _defPre = defPre;
        }

        [Command("songinfo")]
        [Alias("si")]
        [Summary("Gets information about a specific song in the queue.")]
        public async Task CurrentAsync([Summary("The index of the song.")]int songIndex)
        {
            var id = Context.Guild.Id;

            if (!_audio.HasSongs(id))
            {
                await ReplyAsync($"There are no songs in the queue. Try adding some with `{_defPre.Prefix}add`!");
                return;
            }

            var queue = _audio.GetRawQueue(id);

            if (!queue.Any())
            {
                await ReplyAsync($"There are no songs in the queue besides the currently-playing one. Try `{_defPre.Prefix}np`!");
                return;
            }

            if (songIndex > queue.Count + 2 || songIndex < 2)
            {
                await ReplyAsync("Invalid index.");
                return;
            } 

            var song = queue[songIndex - 2];

            await ReplyAsync(embed: new EmbedBuilder()
                .WithTitle("Now Playing")
                .WithFields(new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder().WithName("Title").WithValue(song.Title).WithIsInline(false),
                    new EmbedFieldBuilder().WithName("Author").WithValue(song.Author).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Size").WithValue(_misc.PrettyFormat(song.Size, 3)).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Queuer").WithValue(Context.Guild.GetUser(song.QueuerId).Mention).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("URL").WithValue(song.Url).WithIsInline(true)
                })
                .WithColor(_rand.RandomColor())
                .WithThumbnailUrl(song.ThumbnailUrl)
                .WithCurrentTimestamp()
            .Build());
        }
    }
}