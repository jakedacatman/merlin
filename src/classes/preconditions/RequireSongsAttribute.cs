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
    public class RequireSongsAttribute : PreconditionAttribute
    {
        #pragma warning disable CS1998 //This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!(services.GetService(typeof(AudioService)) as AudioService).HasSongs(context.Guild.Id)) 
                return PreconditionResult.FromError($"There are no songs in the queue. Try adding some with `{(services.GetService(typeof(GuildPrefix)) as GuildPrefix).Prefix}add`!");

            return PreconditionResult.FromSuccess();
        }
        #pragma warning restore CS1998
    }
}