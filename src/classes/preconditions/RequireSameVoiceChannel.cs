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
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var currUser = await context.Guild.GetUserAsync(context.Client.CurrentUser.Id);
            if (currUser.VoiceChannel is not null && (context.User as SocketGuildUser).VoiceChannel == currUser.VoiceChannel) 
                return PreconditionResult.FromSuccess();

            return PreconditionResult.FromError("You must be in the same voice channel as me.");
        }
    }
}