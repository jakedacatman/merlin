using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace merlin.classes
{
    public static class Shell
    {
        public async static Task<string> RunAsync(string command, bool stderr = false)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = stderr,
                    CreateNoWindow = true
                }
            };

            using (proc)
            {
                proc.Start();

                var stdout = await proc.StandardOutput.ReadToEndAsync();

                return stderr ? $"{await proc.StandardError.ReadToEndAsync()}\n{stdout}" : stdout;
            }
        }

        public async static Task<string> FfmpegAsync(string args, bool stderr = false)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = stderr,
                    CreateNoWindow = true
                }
            };

            using (proc)
            {
                proc.Start();

                var stdout = await proc.StandardOutput.ReadToEndAsync();

                return stderr ? $"{await proc.StandardError.ReadToEndAsync()}\n{stdout}" : stdout;
            }
        }
    }
}