using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Fergun.Interactive;
using System.Collections.Generic;
using merlin.services;
using Discord.Net;
using System.Linq;

namespace merlin.commands
{
    [Name("Misc")]
    public class PollCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly MiscService _misc;
        private readonly RandomService _rand;
        private readonly InteractiveService _inter;

        public PollCommand(MiscService misc, InteractiveService inter, RandomService rand)
        {
            _misc = misc;
            _inter = inter;
            _rand = rand;
        }

        [Command("custompoll")]
        [Alias("cpol", "poll2", "pol2")]
        [RequireBotPermission(GuildPermission.AddReactions | GuildPermission.ManageMessages)]
        [Summary("Sends a poll and adds specified reactions for people to vote on.")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task PollAsync([Summary("The question to vote on.")]string message, [Summary("The reactions to vote with.")]params string[] reactions)
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
                await CreatePollAsync(msg, message, emotes);
            }
            catch (HttpException he) when (he.HttpCode == System.Net.HttpStatusCode.BadRequest) 
            {
                await _inter.DelayedSendMessageAndDeleteAsync(Context.Channel, deleteDelay: TimeSpan.FromSeconds(10), text: "One of your reactions was invalid.");
                if (msg != null) await msg.DeleteAsync(); 
            }
        }
        
        [Command("poll")]
        [Alias("pol")]
        [Summary("Sends a poll and adds a thumbs-up and thumbs-down for people to vote on.")]
        [RequireBotPermission(GuildPermission.AddReactions | GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task PollAsync([Summary("The question to vote on."), Remainder]string message)
        {
            IUserMessage msg = null;
            await CreatePollAsync(msg, message, new[] { new Emoji("👍"), new Emoji("👎") });
        }

        private async Task CreatePollAsync(IUserMessage msg, string message, IEnumerable<IEmote> reactions)
        {
            var user = Context.User.Username;

            var em = new EmbedBuilder()
                .WithAuthor(x =>
                {
                    x.Name = $"{user}#{Context.User.Discriminator}";
                    x.IconUrl = Context.User.GetAvatarUrl(size: 512);
                })
                .WithColor(_rand.RandomColor())
                .WithTitle($"{user} asks:")
                .WithDescription(message)
                .WithCurrentTimestamp();

            if (Context.Message is not null) await Context.Message.DeleteAsync();
            
            msg = await ReplyAsync(embed: em.Build());
            await msg.AddReactionsAsync(reactions.ToArray());
        }
    }
}