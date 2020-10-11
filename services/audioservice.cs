using System;
using Discord;
using System.IO;
using System.Net;
using System.Linq;
using Discord.Audio;
using Discord.WebSocket;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

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

            await channel.ConnectAsync(true);
            await channel.DisconnectAsync();
            
            var connection = await channel.ConnectAsync(true, false, true);

            if (!_connections.ContainsKey(id))
                return _connections.TryAdd(channel.Guild.Id, connection);
            else
                return (_connections.TryRemove(id, out var _) && _connections.TryAdd(id, connection));
        }

        public async Task<bool> DisconnectAsync(SocketVoiceChannel channel)
        {
            var id = channel.Guild.Id;

            await channel.DisconnectAsync();

            if (!_connections.TryGetValue(id, out var connection))
                return false;
            connection.Dispose();

            return _connections.TryRemove(id, out var _);
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
                if (!_connections.TryGetValue(id, out var connection))
                    throw new InvalidOperationException("Not connected to a voice channel.");

                var audioUrl = GetUrl(url);

                var req = WebRequest.Create(audioUrl);

                using (var response = await req.GetResponseAsync())
                using (var str = response.GetResponseStream())

                using (var ffmpeg = CreateStream())
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var input = ffmpeg.StandardInput.BaseStream)

                using (var discord = connection.CreatePCMStream(AudioApplication.Mixed))
                {
                    try
                    {
                        var buffer = new byte[4096];
                        var bytesRead = 0;
                        var bytesConverted = 0;

                        while (str.Position <= str.Length)
                        {
                            bytesRead += await str.ReadAsync(buffer, bytesRead, 4096);
                            await input.WriteAsync(buffer, bytesRead, 4096);
                            bytesConverted += await output.ReadAsync(buffer, bytesConverted, 4096);
                            await discord.ReadAsync(buffer, bytesConverted, 4096);
                        }
                    }
                    finally 
                    {
                        await discord.FlushAsync();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public string GetUrl(string ytUrl)
        {

            var info = new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments = $"--no-cache-dir -g {ytUrl}",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            var proc = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = info
            };

            var urls = new List<string>();
            proc.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    urls.Add(e.Data);
            });

            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit(); //blocks :( (good thing we run in RunMode.Async)
            
            var query = urls.Where(x => x.Contains("mime=audio"));
            var url = query.Any() ? query.First() : urls.LastOrDefault();
            return url;
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
    }
}