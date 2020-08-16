using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace donniebot.classes
{
    public static class Shell
    {
        public async static Task<string> Run(string command, bool stderr = false)
        {
            var arr = command.Split(' ');
            var fn = arr[0].Trim('\\');
            var list = new List<string>(arr);
            list.Remove(fn);
            var args = string.Join(' ', list);
            var shell = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fn,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = stderr,
                    CreateNoWindow = true
                }
            };
            Console.WriteLine($"Running {fn} with args {args}");
            shell.Start();

            var stdout = await shell.StandardOutput.ReadToEndAsync();

            return stderr ? $"{await shell.StandardError.ReadToEndAsync()}\n{stdout}" : stdout;
        }
    }
}