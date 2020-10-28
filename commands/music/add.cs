using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.Addons.Interactive;

namespace donniebot.commands
{
    [Name("Audio")]
    public class AddCommand : InteractiveBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public AddCommand(AudioService audio, MiscService misc, RandomService rand)
        {
            _audio = audio;
            _misc = misc;
            _rand = rand;
        }

        [Command("add")]
        [Alias("play")]
        [Summary("Adds a song to the song queue in order to be played.")]
        public async Task AddCmd([Summary("The URL or YouTube search query."), Remainder] string queryOrUrl = null)
        {
            try
            {
                var id = Context.Guild.Id;
                var uId = Context.User.Id;
                var vc = (Context.User as SocketGuildUser).VoiceChannel;
                if (!_audio.IsConnected(id))
                {
                    if (vc == null)
                    {
                        await ReplyAsync("You must be in a voice channel.");
                        return;
                    }
                }

                if (string.IsNullOrWhiteSpace(queryOrUrl))
                {
                    await _audio.PlayAsync(id);
                    
                    return;
                }

                if (Uri.IsWellFormedUriString(queryOrUrl, UriKind.Absolute) && (queryOrUrl.Contains("&list=") || queryOrUrl.Contains("?list=")))
                {
                    var playlist = await _audio.GetPlaylistAsync(queryOrUrl, id, uId);

                    await _audio.EnqueueMany(vc, playlist.Songs);

                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithTitle("Added playlist")
                        .WithFields(new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder().WithName("Title").WithValue(playlist.Title).WithIsInline(false),
                            new EmbedFieldBuilder().WithName("Author").WithValue(playlist.Author).WithIsInline(true),
                            new EmbedFieldBuilder().WithName("Count").WithValue(playlist.Songs.Count).WithIsInline(true),
                            new EmbedFieldBuilder().WithName("URL").WithValue(playlist.Url).WithIsInline(false),
                        })
                        .WithColor(_rand.RandomColor())
                        .WithThumbnailUrl(playlist.ThumbnailUrl)
                        .WithCurrentTimestamp()
                        .Build());

                    return;
                }

                var song = await _audio.CreateSongAsync(queryOrUrl, id, uId);

                await ReplyAsync(embed: new EmbedBuilder()
                    .WithTitle("Added song")
                    .WithFields(new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder().WithName("Title").WithValue(song.Title).WithIsInline(false),
                        new EmbedFieldBuilder().WithName("Author").WithValue(song.Author).WithIsInline(true),
                        new EmbedFieldBuilder().WithName("URL").WithValue(song.Url).WithIsInline(true)
                    })
                    .WithColor(_rand.RandomColor())
                    .WithThumbnailUrl(song.ThumbnailUrl)
                    .WithCurrentTimestamp()
                .Build());

                await _audio.Enqueue(vc, song);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}