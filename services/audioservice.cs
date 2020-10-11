using System;
using Discord;
using System.IO;
using System.Net;
using System.Linq;
using Discord.Audio;
using Discord.WebSocket;
using donniebot.classes;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

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
            
            var connection = await channel.ConnectAsync(true, false, true);

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

                var audioUrl = GetUrl(url);

                var req = WebRequest.Create(audioUrl);

                using (var response = await req.GetResponseAsync())
                using (var str = response.GetResponseStream())

                using (var ffmpeg = CreateStream())
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var input = ffmpeg.StandardInput.BaseStream)

                using (var discord = connection.Stream)
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
                connection.UpdateStream();
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

        private AudioPlayer GetConnection(ulong id) => _connections.First(x => x.GuildId == id);

        private bool HasConnection(ulong id) => _connections.Any(x => x.GuildId == id);
    }
}