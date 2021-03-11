using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Interactivity;
using System.Collections.Generic;
using donniebot.services;
using Discord.WebSocket;
using Discord.Net;

namespace donniebot.commands
{
    [Name("Misc")]
    public class PollCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly InteractivityService _inter;

        public PollCommand(MiscService misc, InteractivityService inter)
        {
            _misc = misc;
            _inter = inter;
        }

        [Command("poll")]
        [Alias("pol")]
        [Summary("Sends a poll and adds reactions for people to vote on.")]
        [Priority(0)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task PollCmd([Summary("The question to vote on.")]string message, [Summary("The reactions to vote with.")]params string[] reactions)
        {
            IUserMessage msg = null;
            try
            {
                var emotes = new List<IEmote>();
                foreach (var r in reactions)
                {
                    if (Emote.TryParse(r, out var res))
                        emotes.Add(res);
                    else 
                        emotes.Add(new Emoji(r));
                }

                await Context.Message.DeleteAsync();
                msg = await ReplyAsync(message);
                await msg.AddReactionsAsync(emotes.ToArray());
            }
            catch (HttpException he) when (he.HttpCode == System.Net.HttpStatusCode.BadRequest) 
            {
                _inter.DelayedSendMessageAndDeleteAsync(Context.Channel, deleteDelay: TimeSpan.FromSeconds(10), text: "One of your reactions was invalid.");
                if (msg != null) await msg.DeleteAsync(); 
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
        [Command("poll")]
        [Alias("pol")]
        [Summary("Sends a poll and adds reactions for people to vote on.")]
        [Priority(1)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task PollCmd([Summary("The question to vote on."), Remainder]string message)
        {
            IUserMessage msg = null;
            try
            {
                await Context.Message.DeleteAsync();
                msg = await ReplyAsync(message);
                await msg.AddReactionsAsync(new[] { new Emoji("👍"), new Emoji("👎") });
            }
            catch (Exception e)
            {
                await ReplyAsync(embed: (await _misc.GenerateErrorMessage(e)).Build());
            }
        }
    }
}