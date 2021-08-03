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
    public class RequireVoiceChannelAttribute : PreconditionAttribute
    {
        #pragma warning disable CS1998 //This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            var id = context.Guild.Id;

            if (user == null) return PreconditionResult.FromError("Cannot execute outside of a guild.");
            
            if (user.VoiceChannel is null) return PreconditionResult.FromError("You must be in a voice channel to run this command.");

            return PreconditionResult.FromSuccess();
        }
        #pragma warning restore CS1998
    }
}