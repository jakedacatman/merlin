using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using merlin.classes;
using System.Collections.Generic;

namespace merlin.services
{
    public class ModerationService
    {
        private readonly DiscordShardedClient _client;
        private readonly DbService _db;

        List<ModerationAction> _actions = new List<ModerationAction>();
        
        public ModerationService(DiscordShardedClient client, DbService db)
        {
            _client = client;
            _db = db;

            _actions.AddRange(_db.LoadActions());
        }

        public async Task<MuteResult> TryMuteUserAsync(SocketGuildUser moderator, SocketGuildUser user, string reason = null, TimeSpan? expiry = null)
        {
            try
            {
                if (user.TimedOutUntil is not null && user.TimedOutUntil.Value.UtcDateTime > DateTime.UtcNow)
                    return MuteResult.FromError("The user is already under a timeout.", user.Id);

                if (expiry is null || expiry > TimeSpan.FromDays(28)) expiry = TimeSpan.FromDays(28);

                await user.SetTimeOutAsync(expiry.Value, new RequestOptions { AuditLogReason = reason });

                var action = new ModerationAction(user.Id, moderator.Id, user.Guild.Id, merlin.classes.ActionType.Mute, expiry, reason);
                AddAction(action);

                return MuteResult.FromSuccess(action);
            }
            catch (Exception e)
            {
                return MuteResult.FromError($"There was an exception while adding a timeout to the user. ({e.Message})", user.Id);
            }
        }

        public async Task<MuteResult> TryUnmuteUserAsync(SocketGuildUser user)
        {
            try
            {
                if (user.TimedOutUntil is null)
                    return MuteResult.FromError("The user is not under a timeout.", user.Id);

                await user.RemoveTimeOutAsync();

                RemoveAction(_actions.First(x => x.Type == classes.ActionType.Mute && x.UserId == user.Id && x.GuildId == user.Guild.Id));
                return MuteResult.FromSuccess("", user.Id);
            }
            catch (Exception e)
            {
                return MuteResult.FromError($"There was an exception while removing the user's timeout. ({e.Message})", user.Id);
            }
        }

        public async Task<int> TryPurgeMessagesAsync(SocketTextChannel channel, int count)
        {
            try
            {
                if (count < 1) count = 1;
                if (count > 1000) count = 1000;

                await channel.DeleteMessagesAsync(await channel.GetMessagesAsync(count + 1).FlattenAsync());

                return count;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }
        public async Task<int> TryPurgeMessagesAsync(SocketTextChannel channel, int count, SocketGuildUser user)
        {
            try
            {
                if (count < 1) count = 1;
                if (count > 100) count = 100;

                var msgs = (await channel.GetMessagesAsync(100).FlattenAsync()).Where(x => x.Author == user).OrderByDescending(x => x.CreatedAt).Take(count);
                await channel.DeleteMessagesAsync(msgs);

                return msgs.Count();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }

        public List<ModerationAction> GetActionsForUser(ulong uId) => _actions.Where(x => x.UserId == uId).ToList();

        public void AddAction(ModerationAction action)
        {
            _actions.Add(action);
            _db.AddAction(action);
        }

        public void RemoveAction(ModerationAction action)
        {
            _actions.Remove(action);
            _db.RemoveAction(action);
        }
        public void RemoveAction(Func<ModerationAction, bool> query)
        {
            var action = _actions.First(query);

            _actions.Remove(action);
            _db.RemoveAction(action);
        }
    }
}
