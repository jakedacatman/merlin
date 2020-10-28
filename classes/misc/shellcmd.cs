using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace donniebot.classes
{
    public static class Shell
    {
        public async static Task<string> Run(string command, bool stderr = false)
        {
            var shell = new Process
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
            shell.Start();

            var stdout = await shell.StandardOutput.ReadToEndAsync();

            return stderr ? $"{await shell.StandardError.ReadToEndAsync()}\n{stdout}" : stdout;
        }
    }
}