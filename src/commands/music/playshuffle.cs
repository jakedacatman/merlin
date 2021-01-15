using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using donniebot.classes;
using System.Threading.Tasks;
using System.Collections.Generic;
using Interactivity;
using LiteDB;
using System.Linq;

namespace donniebot.commands
{
    [Name("Audio")]
    public class PlayShuffleCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly AudioService _audio;
        private readonly MiscService _misc;
        private readonly ulong _roleId;
        private readonly ulong[] _users;

        public PlayShuffleCommand(AudioService audio, MiscService misc, DbService db)
        {
            _audio = audio;
            _misc = misc;

            var id = Context?.Guild.Id ?? 0;
            _roleId = db.GetItem<DjRole>("djroles", Query.EQ("GuildId", id))?.RoleId ?? 0;
            _users = _audio.GetListeningUsers(id).Select(x => x.Id).ToArray();
        }

        [Command("playshuffle")]
        [Alias("ps", "playsh", "plsh")]
        [Summary("Adds a song or playlist to the queue, then shuffles the queue.")]
        [RequireDjRole]
        public async Task PlayShuffleCmd([Summary("The URL or YouTube search query."), Remainder] string queryOrUrl = null)
        {
            try
            {
                await _audio.AddAsync(Context.User as SocketGuildUser, Context.Channel as SocketTextChannel, queryOrUrl, true);
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}