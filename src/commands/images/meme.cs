using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using donniebot.services;
using Interactivity;
using Discord;

namespace donniebot.commands
{
    [Name("Image")]
    public class MemeCommand : ModuleBase<ShardedCommandContext>
    {
        private readonly DiscordShardedClient _client;
        private readonly ImageService _img;
        private readonly MiscService _misc;
        private readonly RandomService _rand;

        public MemeCommand(DiscordShardedClient client, ImageService img, MiscService misc, RandomService rand)
        {
            _client = client;
            _img = img;
            _misc = misc;
            _rand = rand;
        }

        [Command("meme")]
        [Alias("m")]
        [Summary("Takes the last-sent message and memes it.")]
        public async Task MemeAsync()
        {
            var previousMsg = await _misc.GetPreviousMessageAsync(Context.Channel as SocketTextChannel);

            if (string.IsNullOrEmpty(previousMsg.Content)) return;

            var url = (await _img.GetRedditImageAsync("earthporn", Context.Guild.Id, false)).Url;   
            var img = await _img.DrawTextAsync(url, previousMsg.Content.Replace("\\", ""));
            img = _img.BackgroundColor(img, _rand.RandomNumber(0, 255), _rand.RandomNumber(0, 255), _rand.RandomNumber(0, 255));
            img = _img.Saturate(img, _rand.RandomFloat(5));
            img = _img.Brightness(img, _rand.RandomFloat(1, 5));
            img = _img.Blur(img, _rand.RandomFloat(5));
            img = _img.Pixelate(img, _rand.RandomNumber(1, 4));
            img = _img.Sharpen(img, _rand.RandomFloat(5));
            img = _img.Jpeg(img, _rand.RandomNumber(1, 15));

            await _img.SendToChannelAsync(img, Context.Channel, new MessageReference(Context.Message.Id));
        }
    }
}