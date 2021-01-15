using System;
using Discord;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using donniebot.services;
using System.Threading.Tasks;
using System.Collections.Generic;
using LiteDB;

namespace donniebot.classes
{
    public class RequireDjRoleAttribute : PreconditionAttribute
    {
        #pragma warning disable CS1998 //This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            var id = context.Guild.Id;

            var roleId = (services.GetService(typeof(DbService)) as DbService)
                .GetItem<DjRole>("djroles", Query.EQ("GuildId", id))?.RoleId ??
                    user.Guild.Roles.FirstOrDefault(x => x.Name == "DJ")?.Id ?? 0;

            if (user == null) return PreconditionResult.FromError("Cannot execute outside of a guild");

            var users = (services.GetService(typeof(AudioService)) as AudioService).GetListeningUsers(id);

            if (users.Count == 1 && users[0].Id == user.Id)
                return PreconditionResult.FromSuccess();

            if (user.Roles.Any(x => x.Id == roleId)) return PreconditionResult.FromSuccess();

            if (user.GuildPermissions.MuteMembers) return PreconditionResult.FromSuccess();

            if (!user.Guild.Roles.Any(x => x.Id == roleId)) return PreconditionResult.FromError("The role does not exist.");

            return PreconditionResult.FromError("Try this command when you are alone with the bot or if you have the Mute Members permission. Having the DJ role works as well.");
        }
        #pragma warning restore CS1998
    }
}