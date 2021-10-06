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
    public class RequireSameVoiceChannelAttribute : PreconditionAttribute
    {
        #pragma warning disable CS1998 //This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var currUser = context.Client.CurrentUser as SocketGuildUser;
            if (currUser.VoiceChannel is not null && (context.User as SocketGuildUser).VoiceChannel == currUser.VoiceChannel) 
                return PreconditionResult.FromSuccess();

            return PreconditionResult.FromError("You must be in the same voice channel as me.");
        }
        #pragma warning restore CS1998
    }
}