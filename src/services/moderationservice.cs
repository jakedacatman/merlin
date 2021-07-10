using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using donniebot.classes;
using LiteDB;
using System.Collections.Generic;

namespace donniebot.services
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

            _client.ShardReady += (DiscordSocketClient client) => 
            {
                if (client.ShardId >= _client.Shards.Count - 1)
                {
                    var complete = Task.Run(async () => 
                    {
                        while (true)
                        {
                            //var counter = 0;

                            foreach (var action in _actions.ToList())
                                if (action.Expiry.HasValue && action.Expiry <= DateTime.UtcNow)
                                {
                                    var guild = _client.Guilds.First(x => x.Id == action.GuildId);
                                    var user = guild.Users.First(x => x.Id == action.UserId);
                            
                                    switch (action.Type)
                                    {
                                        case classes.ActionType.Mute:
                                        {
                                            await TryUnmuteUserAsync(guild, guild.Users.First(x => x.Id == action.UserId));
                                            break;
                                        }
                                        case classes.ActionType.Ban:
                                        {
                                            await guild.RemoveBanAsync(user);
                                            break;
                                        }
                                    }

                                    //counter++;
                                    RemoveAction(action);
                            }

                            //Console.WriteLine($"Reversed {counter} actions.");
                            await Task.Delay(1000);
                        }
                    });
                }

                return Task.CompletedTask;
            };
        }

        public async Task<MuteResult> TryMuteUserAsync(SocketGuild guild, SocketGuildUser moderator, SocketGuildUser user, string reason = null, TimeSpan? expiry = null)
        {
            try
            {
                IRole role;

                if (guild.Roles.Any(x => x.Name == "Muted"))
                    role = guild.Roles.First(x => x.Name == "Muted");
                else
                {
                    OverwritePermissions Permissions = new OverwritePermissions(addReactions: PermValue.Deny, sendMessages: PermValue.Deny, attachFiles: PermValue.Deny, useExternalEmojis: PermValue.Deny, speak: PermValue.Deny);

                    role = await guild.CreateRoleAsync("Muted", GuildPermissions.None, Color.Default, false, false);

                    await role.ModifyAsync(x => x.Position = guild.GetUser(_client.CurrentUser.Id).Roles.OrderBy(y => y.Position).Last().Position);

                    foreach (var channel in (guild as SocketGuild).TextChannels)
                        if (!channel.PermissionOverwrites.Select(x => x.Permissions).Contains(Permissions))
                            await channel.AddPermissionOverwriteAsync(role, Permissions);

                    foreach (var channel in (guild as SocketGuild).VoiceChannels)
                        if (!channel.PermissionOverwrites.Select(x => x.Permissions).Contains(Permissions))
                            await channel.AddPermissionOverwriteAsync(role, Permissions);
                }

                if (role.Position < user.Roles.OrderBy(x => x.Position).Last().Position)
                    return MuteResult.FromError("The \"Muted\" role is below the user's highest role.", user.Id);

                if (user.Roles.Contains(role)) return MuteResult.FromError("The user is already muted.", user.Id);

                await user.AddRoleAsync(role);
                
                await user.ModifyAsync(x => x.Mute = true);

                var action = new ModerationAction(user.Id, moderator.Id, guild.Id, donniebot.classes.ActionType.Mute, expiry, reason);
                AddAction(action);

                return MuteResult.FromSuccess(action);
            }
            catch (Exception e)
            {
                return MuteResult.FromError($"There was an exception while muting the user. ({e.Message})", user.Id);
            }
        }

        public async Task<MuteResult> TryUnmuteUserAsync(SocketGuild guild, SocketGuildUser user)
        {
            try
            {
                SocketRole role;

                if (guild.Roles.Any(x => x.Name == "Muted"))
                    role = guild.Roles.FirstOrDefault(x => x.Name == "Muted");
                else return MuteResult.FromError("There is not a \"Muted\" role in the server.", user.Id);

                if (role.Position < user.Roles.OrderBy(x => x.Position).Last().Position)
                    return MuteResult.FromError("The \"Muted\" role is below the user's highest role.", user.Id);

                if (!user.Roles.Contains(role)) return MuteResult.FromError("The user is already unmuted.", user.Id);

                await user.RemoveRoleAsync(role);

                RemoveAction(x => 
                    x.UserId == user.Id && 
                    x.GuildId == guild.Id && 
                    x.Type == donniebot.classes.ActionType.Mute
                );

                await user.ModifyAsync(x => x.Mute = false);
                return MuteResult.FromSuccess("", user.Id);
            }
            catch (Exception e)
            {
                return MuteResult.FromError($"There was an exception while unmuting the user. ({e.Message})", user.Id);
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
