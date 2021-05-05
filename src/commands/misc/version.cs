using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Interactivity;
using System.Collections.Generic;
using System.Linq;
using donniebot.services;
using Discord.WebSocket;
using Discord.Net;
using System.IO;

namespace donniebot.commands
{
    [Name("Misc")]
    public class VersionCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public VersionCommand(MiscService misc, RandomService rand)
        {
            _misc = misc;
            _rand = rand;
        }

        [Command("version")]
        [Alias("ver", "v")]
        [Summary("Gets the bot's current version as determined by its latest git commit.")]
        public async Task VersionAsync()
        {
            try
            {
                if (!File.Exists(".version"))
                    await ReplyAsync("No version file found. If you are the bot owner, make sure that the .version file from the git repository is copied over to the same directory as the bot executable.");
                else
                {
                    var lines = await File.ReadAllLinesAsync(".version"); //echo `date` > .version && git log --date=iso >> .version
                    var commit = lines[1].Substring(7); //commit <commit>
                    var author = lines[2].Substring(8); //Author: <author> <email>
                    author = author.Substring(0, author.IndexOf(' '));
                    var date = lines[3].Substring(8); //Date:   <date in iso format>
                    
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithTitle($"Commit {commit.Substring(0, 7)}") //7 character commit string like github
                        .WithColor(_rand.RandomColor())
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName($"Author: {author}")
                            .WithUrl($"https://github.com/{author}") //their profile
                        )
                        .WithFooter(new EmbedFooterBuilder()
                            .WithText($"Published at {DateTime.Parse(date, styles: System.Globalization.DateTimeStyles.AdjustToUniversal )}") //utc
                        )
                        .WithFields(new List<EmbedFieldBuilder>
                        {
                            new EmbedFieldBuilder().WithName("Message").WithValue(lines[5].Substring(4)).WithIsInline(false) //commit message
                        })
                        .Build()
                    );
                }
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessageAsync(e)).Build());
            }
        }
    }
}