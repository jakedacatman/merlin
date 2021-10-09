using System;
using System.IO;
using System.Linq;
using Discord.Audio;
using System.Threading;
using Nerdbank.Streams;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace donniebot.classes
{
    public class AudioPlayer : IDisposable
    {
        public ulong GuildId { get; }
        public IAudioClient Connection { get; private set; }
        public SocketVoiceChannel VoiceChannel { get; private set; }
        public SocketTextChannel TextChannel { get; }
        public AudioOutStream Stream { get; private set; }
        public Song Current { get; set; } = null;
        public List<Song> Queue { get; set; }
        public TimeSpan Position { 
            get 
            {
                if (Current == null) return TimeSpan.FromSeconds(0);
                return TimeSpan.FromSeconds(BytesPlayed / 196608d); //ffmpeg output in bytes/second
            }
        }
        public bool HasDisconnected { get; private set; } = false;
        public bool IsPlaying { get; set; } = false;
        public bool IsPaused { get; private set; } = false;
        public bool IsLooping { get; private set; } = false;

        private int _skips = 0;
        private List<ulong> _skippedUsers = new List<ulong>();

        internal uint BytesDownloaded { get; set; } = 0;
        internal uint BytesPlayed { get; set; } = 0;

        public event Func<Song, Task> SongEnded;
        public event Func<Song, Task> SongSkipped;
        public event Func<bool, Task> SongToggledPlaying;

        public AudioPlayer(ulong id, SocketVoiceChannel channel, IAudioClient client, SocketTextChannel textchannel)
        {
            GuildId = id;
            VoiceChannel = channel;
            Connection = client;
            TextChannel = textchannel;
            Stream = client.CreatePCMStream(AudioApplication.Mixed);
            Queue = new List<Song>();
        }

        public void ToggleLoop() => IsLooping = !IsLooping;

        public void Enqueue(Song s, int? position)
        { 
            if (!position.HasValue)
                Queue.Add(s);
            else
                Queue.Insert(position.Value, s);
        }

        public void EnqueueMany(IEnumerable<Song> s, int? position)
        {
            if (!position.HasValue)
                Queue.AddRange(s);
            else
                Queue.InsertRange(position.Value, s);
        }

        public async Task LeaveAsync(bool sendMessage = true, ISocketMessageChannel tc = null)
        {
            _skips = 0;
            Queue.RemoveAll(x => x.GuildId >= 0);
            SongSkipped?.Invoke(this.Current);

            if (sendMessage)
            {
                if (tc != null)
                    await tc.SendMessageAsync("👋");
                else 
                    await TextChannel.SendMessageAsync("👋");
            }

            HasDisconnected = true;
        }

        public Song Pop()
        {
            var song = Queue[0];
            Queue.Remove(song);
            return song;
        }
        public void Dequeue(int id)
        {
            if (id >= 0 && id < Queue.Count)
                Queue.RemoveAt(id);
        }

        public void Shuffle()
        {
            if (Queue.Any())
                Queue = Queue.Shuffle().ToList();
        }

        public async Task UpdateAsync(SocketVoiceChannel channel)
        {
            VoiceChannel = channel;
            await channel.DisconnectAsync();
            Connection = await channel.ConnectAsync(true, false);
            Stream = Connection.CreatePCMStream(AudioApplication.Mixed);
            HasDisconnected = false;
        }

        public async Task ResumeAsync()
        {
            if (!IsPlaying) return;
            if (!IsPaused)
            {
                await TextChannel.SendMessageAsync("Playback has already resumed!");
                return;
            }

            IsPaused = false;
            SongToggledPlaying?.Invoke(IsPaused);
            await TextChannel.SendMessageAsync("Resuming playback.");
        }

        public async Task PauseAsync()
        {
            if (!IsPlaying) return;
            if (IsPaused)
            {
                await TextChannel.SendMessageAsync("Playback is already paused!");
                return;
            }

            IsPaused = true;
            SongToggledPlaying?.Invoke(IsPaused);
            await TextChannel.SendMessageAsync("Pausing playback.");
        }

        internal async Task<int> DoSkipAsync()
        {
            _skips = 0;
            SongSkipped?.Invoke(Current);
            await TextChannel.SendMessageAsync("Skipping the current song.");
            return 0;
        }

        public IEnumerable<SocketGuildUser> GetListeningUsers() => VoiceChannel.Users.Where(x => 
            !x.IsBot && 
            !x.IsDeafened &&
            !x.IsSelfDeafened);

        public async Task<int> SkipAsync(SocketGuildUser skipper)
        {
            if (!IsPlaying)
            {
                await TextChannel.SendMessageAsync("I am not playing anything right now.");
                return _skips;
            }

            var listeningUsers = GetListeningUsers();
            var requiredCount = (int)Math.Floor(.75f * listeningUsers.Count());
            
            if (skipper == VoiceChannel.Guild.CurrentUser) return await DoSkipAsync();
            if (skipper.GuildPermissions.MuteMembers) return await DoSkipAsync();
            if (listeningUsers.Count() == 1 && listeningUsers.First() == skipper)  return await DoSkipAsync();
            if (_skips >= requiredCount)  return await DoSkipAsync();

            if (_skippedUsers.Contains(skipper.Id))
            {
                await TextChannel.SendMessageAsync("You have already voted to skip the current song.");
                return _skips;
            }

            if (skipper.VoiceChannel != VoiceChannel)
            {
                await TextChannel.SendMessageAsync("You are not in the voice channel.");
                return _skips;
            }

            _skips++;
            _skippedUsers.Add(skipper.Id);

            await TextChannel.SendMessageAsync($"Voted to skip the current song. ({_skips} votes/{requiredCount} required)");
            return _skips;
        }

        public async Task PlayAsync()
        {
            if (VoiceChannel is null)
                throw new InvalidOperationException("Not connected to a voice channel.");

            if (!Queue.Any())
                return;

            Current = Pop();
            
            try
            {
                var botVc = VoiceChannel.Guild.CurrentUser.VoiceChannel;
                if (botVc != VoiceChannel)
                {
                    await botVc.DisconnectAsync();
                    Connection = await VoiceChannel.ConnectAsync(true, false);
                }

                if (IsPlaying)
                    return;

                if (HasDisconnected)
                    await UpdateAsync(VoiceChannel);

                var info = Current.Info ?? await GetAudioInfoAsync(Current.Url);

                Current.Size = info.Size.Bytes;

                using (var str = await GetAudioAsync(info))
                using (var downloadStream = new SimplexStream())
                using (var ffmpeg = CreateStream())
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var input = ffmpeg.StandardInput.BaseStream)
                {
                    IsPlaying = true;

                    const int block_size = 4096; //4 KiB

                    var bufferDown = new byte[block_size];
                    var bufferRead = new byte[block_size];
                    var bufferWrite = new byte[block_size];

                    int bytesDown = 0, bytesRead = 0, bytesConverted = 0;

                    var pauseCts = new CancellationTokenSource();

                    SongToggledPlaying += (bool state) =>
                    {
                        if (state)
                            pauseCts.Cancel();
                        else
                            pauseCts = new CancellationTokenSource();

                        return Task.CompletedTask;
                    };

                    async Task Download(CancellationToken token)
                    {
                        try
                        {
                            do
                            {
                                if (token.IsCancellationRequested) break;

                                bytesDown = await str.ReadAsync(bufferDown, 0, block_size);
                                BytesDownloaded += (uint)bytesDown;

                                await downloadStream.WriteAsync(bufferDown, 0, bytesDown);
                            }
                            while (bytesDown > 0);
                        }
                        finally
                        {
                            downloadStream.CompleteWriting();
                        }
                    }

                    async Task Read(CancellationToken token)
                    {
                        try
                        {
                            do
                            {
                                if (token.IsCancellationRequested) break;

                                bytesRead = await downloadStream.ReadAsync(bufferRead, 0, block_size);
                                await input.WriteAsync(bufferRead, 0, bytesRead);
                            }
                            while (bytesRead > 0);
                        }
                        catch (IOException)
                        {

                        }
                    }

                    var hasHadFullChunkYet = false;
                    async Task Write(CancellationToken token)
                    {
                        try
                        {
                            do
                            {
                                while (IsPaused) //don't write to discord while paused
                                {
                                    try
                                    {
                                        if (token.IsCancellationRequested) return;

                                        await Task.Delay(-1, pauseCts.Token); //wait forever (until token is called)
                                    }
                                    catch (TaskCanceledException)
                                    {

                                    }
                                }
                                
                                if (token.IsCancellationRequested) break;

                                if (hasHadFullChunkYet && bytesConverted < block_size) //if bytesConverted is less than here, then the last (small) chunk is done
                                    break;

                                bytesConverted = await output.ReadAsync(bufferWrite, 0, block_size);
                                BytesPlayed += (uint)bytesConverted;

                                if (bytesConverted == block_size)
                                    hasHadFullChunkYet = true;

                                await Stream.WriteAsync(bufferWrite, 0, bytesConverted);
                            }
                            while (bytesConverted > 0);
                        }
                        finally
                        {
                            ffmpeg.Kill();
                        }
                    }

                    try //allows for skipping
                    {
                        var cts = new CancellationTokenSource();

                        SongSkipped += (Song _) =>
                        {
                            cts.Cancel();
                            return Task.CompletedTask;
                        };

                        await Task.WhenAll(Download(cts.Token), Read(cts.Token), Write(cts.Token));
                    }
                    catch (Exception)
                    {

                    }
                    finally
                    {
                        await Stream.FlushAsync();

                        if (!ffmpeg.HasExited) 
                            ffmpeg.Kill();
                    }
                }
            }
            catch (Discord.Net.WebSocketClosedException)
            {
                Dispose();
                return;
            }
            finally
            {
                BytesDownloaded = 0;
                BytesPlayed = 0;
                IsPlaying = false;
                SongEnded?.Invoke(Current);

                Current = null;
            }
        }


        #pragma warning disable VSTHRD100 //Avoid "async void" methods, because any exceptions not handled by the method will crash the process. (what else can i do? if i make it return Task the inheritance does not work)
        public async void Dispose()
        {
            HasDisconnected = true;
            IsPlaying = false;
            IsPaused = false;
            await VoiceChannel.DisconnectAsync();
            Connection.Dispose();
            await Stream.DisposeAsync();
        }
        #pragma warning restore VSTHRD100
    }
}
