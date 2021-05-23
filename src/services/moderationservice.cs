using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using donniebot.classes;
using LiteDB;

namespace donniebot.services
{
    public class ModerationService
    {
        private readonly DiscordShardedClient _client;
        private readonly DbService _db;
        
        public ModerationService(DiscordShardedClient client, DbService db)
        {
            _client = client;
            _db = db;
        }

        public async Task<MuteResult> TryMuteUserAsync(SocketGuild guild, SocketGuildUser moderator, SocketGuildUser user)
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
                
                //await user.ModifyAsync(x => x.Mute = true);
                return MuteResult.FromSuccess("", user.Id);
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

                //await user.ModifyAsync(x => x.Mute = false);
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
    }
}