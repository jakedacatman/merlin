using System;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace donniebot.services
{
    public class AudioService
    {
        private ConcurrentDictionary<ulong, IAudioClient> _connections;
        private readonly DiscordShardedClient _client;

        public AudioService(DiscordShardedClient client)
        {
            _connections = new ConcurrentDictionary<ulong, IAudioClient>();
            _client = client;
        }

        public async Task<bool> ConnectAsync(SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;
            var connection = await channel.ConnectAsync(true, false, true);

            if (!_connections.ContainsKey(id))
                return _connections.TryAdd(channel.Guild.Id, connection);
            else if (!_connections.TryGetValue(id, out var comp))
                return false;
            else
                return _connections.TryUpdate(id, connection, comp);
        }

        public async Task<bool> DisconnectAsync(SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;

            if (!_connections.TryGetValue(id, out var connection))
                return false;
            
            await channel.DisconnectAsync();
            connection.Dispose();

            return _connections.TryRemove(id, out var _);
        }
    }
}