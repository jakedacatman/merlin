using System;
using System.IO;
using System.Linq;
using YoutubeExplode;
using Nerdbank.Streams;
using Discord.WebSocket;
using donniebot.classes;
using System.Diagnostics;
using YoutubeExplode.Videos;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace donniebot.services
{
    public class AudioService
    {
        private List<AudioPlayer> _connections = new List<AudioPlayer>();
        private readonly DiscordShardedClient _client;

        public AudioService(DiscordShardedClient client)
        {
            _client = client;
        }

        public async Task ConnectAsync(SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;

            await channel.ConnectAsync(true);
            await channel.DisconnectAsync();
            
            var connection = await channel.ConnectAsync(true, false);

            if (!HasConnection(id))
                _connections.Add(new AudioPlayer(id, connection));
            else
            {
                var curr = GetConnection(id);
                _connections.Remove(curr);
                _connections.Add(new AudioPlayer(id, connection));
            }
        }

        public async Task DisconnectAsync(SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;

            await channel.DisconnectAsync();

            if (HasConnection(id))
            {
                var connection = GetConnection(id);
                _connections.Remove(connection);
                connection.Dispose();
            }
        }


        public async Task PlayAsync(SocketVoiceChannel channel, string url)
        {
            await ConnectAsync(channel);
            await PlayAsync(channel.Guild.Id, url);
        }
        public async Task PlayAsync(ulong id, string url)
        {
            try
            {
                if (!HasConnection(id))
                    throw new InvalidOperationException("Not connected to a voice channel.");

                var connection = GetConnection(id);

                using (var str = await GetAudioAsync(url))
                using (var downloadStream = new SimplexStream())
                using (var ffmpeg = CreateStream())
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var input = ffmpeg.StandardInput.BaseStream)

                using (var discord = connection.Stream)
                {
                    try
                    {
                        int block_size = 4096;

                        var bufferDown = new byte[block_size];
                        var bufferRead = new byte[block_size];
                        var bufferWrite = new byte[block_size];

                        var bytesDown = 0;
                        var bytesRead = 0;
                        var bytesConverted = 0;
                        
                        var download = Task.Run(async () => 
                        {
                            do
                            {
                                bytesDown = await str.ReadAsync(bufferDown, 0, block_size);
                                await downloadStream.WriteAsync(bufferDown, 0, bytesDown);
                            }
                            while (bytesDown > 0);

                            downloadStream.CompleteWriting();
                        });

                        var read = Task.Run(async () => 
                        {
                            do
                            {
                                bytesRead = await downloadStream.ReadAsync(bufferRead, 0, block_size);
                                await input.WriteAsync(bufferRead, 0, bytesRead);
                            }
                            while (bytesRead > 0);
                        });

                        var write = Task.Run(async () => 
                        {
                            do 
                            {
                                bytesConverted = await output.ReadAsync(bufferWrite, 0, block_size);
                                await discord.WriteAsync(bufferWrite, 0, bytesConverted);
                            }
                            while (bytesConverted > 0);
                        });

                        Task.WaitAll(download, read, write);

                    }
                    finally 
                    {
                        await discord.FlushAsync();
                    }
                }
                connection.UpdateStream();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task<Stream> GetAudioAsync(string ytUrl)
        {
            var yt = new YoutubeClient();
            var info = await yt.Videos.Streams.GetManifestAsync(new VideoId(ytUrl));
            
            return await yt.Videos.Streams.GetAsync(info.GetAudioOnly().OrderByDescending(x => x.Bitrate).First());
        }

        private Process CreateStream()
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            });
        }

        private AudioPlayer GetConnection(ulong id) => _connections.First(x => x.GuildId == id);

        private bool HasConnection(ulong id) => _connections.Any(x => x.GuildId == id);
    }
}