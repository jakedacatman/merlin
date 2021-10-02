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
using donniebot.classes;

namespace donniebot.commands
{
    [Name("Misc")]
    public class VersionCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly RandomService _rand;
        private readonly NetService _net;

        public VersionCommand(MiscService misc, RandomService rand, NetService net)
        {
            _misc = misc;
            _rand = rand;
            _net = net;
        }

        [Command("version")]
        [Alias("ver", "v")]
        [Summary("Gets the bot's current version as determined by its latest git commit.")]
        public async Task VersionAsync()
        {
            if (!File.Exists(".version"))
                await ReplyAsync("No version file found. If you are the bot owner, make sure that the .version file from the git repository is copied over to the same directory as the bot executable.");
            else
            {
                var currVer = ParseFile(await File.ReadAllLinesAsync(".version")); //echo `date` > .version && git log --date=iso >> .version
                var latestVer = ParseFile((await _net.DownloadAsStringAsync("https://raw.githubusercontent.com/jakedacatman/donniebot/master/.version")).Split('\n'));
                
                await ReplyAsync(embed: new EmbedBuilder()
                    .WithTitle($"Commit {currVer.Commit.Substring(0, 7)}") //7 character commit string like github
                    .WithColor(_rand.RandomColor())
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName($"Author: {currVer.Author}")
                        .WithUrl($"https://github.com/{currVer.Author}") //their profile
                    )
                    .WithFooter(new EmbedFooterBuilder()
                        .WithText($"Published at {currVer.Date}{(currVer.Commit == latestVer.Commit ? "" : $" | Out of date; latest version is {latestVer.Commit.Substring(0, 7)}")}") //utc
                    )
                    .WithFields(new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder().WithName("Message").WithValue(currVer.Message).WithIsInline(false) //commit message
                    })
                    .Build()
                );
            }
        }

        private VersionFile ParseFile(string[] text)
        {
            var commit = text[1].Substring(7); //commit <commit>
            var author = text[2].Substring(8); //Author: <author> <email>
            author = author.Substring(0, author.IndexOf(' '));
            var date = DateTime.Parse(text[3].Substring(8), styles: System.Globalization.DateTimeStyles.AdjustToUniversal); //Date:   <date in iso format>
            var message = text[5].Substring(4);

            return new VersionFile
            {
                Commit = commit,
                Author = author,
                Date = date,
                Message = message
            };
        }
    }
}