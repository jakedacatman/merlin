using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using donniebot.classes;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;
using Discord.Commands;
using MoonSharp.Interpreter;

namespace donniebot.services
{
    public class MiscService
    {
        private readonly DiscordShardedClient _client;
        private IServiceProvider _services;
        private readonly Random _random;
        private List<int> ids = new List<int>();
        private readonly HttpClient _hc;
        private readonly DbService _db;
        private readonly string uploadKey;

        private readonly Dictionary<Type, string> _aliases = new Dictionary<Type, string>()
        {
            { typeof(double), "number" },
            { typeof(string), "string" },
            { typeof(Dictionary<object, object>), "table" },
            { typeof(Dictionary<DynValue, DynValue>), "table" },
            { typeof(Closure), "function" },
            { typeof(Coroutine), "thread" },
            { typeof(DynValue[]), "tuple" },
            { typeof(bool), "boolean" },
            { typeof(UserData), "userdata" }
        };

        public MiscService(DiscordShardedClient client, IServiceProvider services, Random random, DbService db)
        {
            _client = client;
            _services = services;
            _random = random;
            _hc = new HttpClient();
            _db = db;
            uploadKey = _db.GetApiKey("upload");
        }

        private readonly string[] errorMessages = new string[]
        {
            "Whoops!",
            "Sorry!",
            "An error has occured.",
            "well frick",
            "Okay, this is not epic",
            "Uh-oh!",
            "Something went wrong.",
            "Oh snap!",
            "Oops!",
            "Darn...",
            "I can't believe you've done this.",
            "SAD!",
            "Thank you Discord user, very cool",
            "bruh",
            "HTTP 418 I'm a teapot",
            "I don't feel so good...",
            "On your left!",
            "[insert funny phrase]",
            "This wasn't supposed to happen.",
            "How could you, you monster?",
            "Don't worry, everything is normal.",
            "Did you *have* to do that?",
            "Why...?",
            "Here's your gift for being such a dear user!",
            "This isn't my fault... I swear!",
            "You're doing very well!",
            "Is that a JoJo reference?",
            "Is this funny to you?"
        };

        public async Task<EmbedBuilder> GenerateErrorMessage(Exception e)
        {
            var description = "";
            if (e.Message == "The server responded with error 40005: Request entity too large")
                description = "The resulting file was too large to upload to Discord.";
            else if (e.Message == "Try the command with a url, or attach an image." || e.Message == "Text cannot be blank.")
                description = e.Message;
            else if (e.Message == "Name or service not known")
                description = "That website does not exist.";
            else if (e is SixLabors.ImageSharp.UnknownImageFormatException)
                description = "The image format was not valid.";
            else
            {
                description = "This command has thrown an exception. Here is ";

                var ie = e.InnerException;
                string message = e.Message;
                if (ie != null) message += $"\n*(inner: {ie.Message})*";

                if (message.Length < 1000)
                    description += $"its message:\n**{message.Replace("`", @"\`")}**";
                else
                    description += $"a [link]({await UploadToPastebinAsync(message)} to its message.";

                description += "\nStack trace:\n";

                string trace = e.StackTrace;
                if (ie != null) trace += $"\ninner: {ie.StackTrace}";

                if (trace.Length < 1000)
                    description += $"```{trace.Replace("`", @"\`")}```";
                else
                    description += $"[here]({await UploadToPastebinAsync(trace)})";
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(RandomColor())
                .WithCurrentTimestamp()
                .WithFooter(e.GetType().ToString())
                .WithDescription(description)
                .WithTitle(errorMessages[_random.Next(errorMessages.Length)]);

            return embed;
            
        }

        public Color RandomColor()
        {
            Random r = new Random();
            uint clr = Convert.ToUInt32(r.Next(0, 0xFFFFFF));
            return new Color(clr);
        }

        public async Task<EmbedBuilder> EvaluateAsync(ShardedCommandContext context, string code)
        {
            Discord.Rest.RestUserMessage msg = await context.Channel.SendMessageAsync("Working...");

            code = code.Replace("“", "\"").Replace("‘", "\'").Replace("”", "\"").Replace("’", "\'").Trim('`');
            if (code.Length > 2 && code.Substring(0, 2) == "cs") code = code.Substring(2);

            IEnumerable<Assembly> assemblies = GetAssemblies();

            var sb = new StringBuilder();

            var globals = new Globals
            {
                _client = _client,
                _context = context,
                _guild = context.Guild,
                _channel = context.Channel,
                _user = context.User as SocketGuildUser,
                _services = _services,
                _message = context.Message,
                Console = new FakeConsole(sb),
                _db = _services.GetService<DbService>(),
                _misc = this,
                Random = _random,
                _commands = _services.GetService<CommandService>(),
                _img = _services.GetService<ImageService>(),
            };
            globals._globals = globals;
            
            var options = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
                .AddReferences(assemblies)
                .AddImports(globals.Imports)
                .WithAllowUnsafe(true)
                .WithLanguageVersion(LanguageVersion.CSharp8);

            Stopwatch s = Stopwatch.StartNew();
            var script = CSharpScript.Create(code, options, typeof(Globals));
            var compile = script.GetCompilation().GetDiagnostics();
            var cErrors = compile.Where(x => x.Severity == DiagnosticSeverity.Error);
            s.Stop();

            if (cErrors.Any())
            {
                await msg.DeleteAsync();
                return await GenerateErrorAsync(code, cErrors, "cs");
            }

            ScriptState<object> eval;
            Stopwatch c = Stopwatch.StartNew();
            try
            {
                eval = await script.RunAsync(globals);
            }
            catch (Exception e)
            {
                await msg.DeleteAsync();
                return await GenerateErrorAsync(code, e, "cs");
            }
            c.Stop();

            var result = eval.ReturnValue;
            if (eval.Exception != null)
            {
                await msg.DeleteAsync();
                return await GenerateErrorAsync(code, eval.Exception, "cs");
            }

            string description;
            if (code.Length < 1000)
                 description = $"in: ```cs\n{code}```\nout: \n";
            else
                description = $"in: **[input]({await UploadToPastebinAsync(code)})**\nout: \n";
            string tostringed = result == null ? " " : result.ToString();

            if (result is ICollection r)
                tostringed = r.MakeString();
            else if (result is IReadOnlyCollection<object> x)
                tostringed = x.MakeString();
            else if (result is Dictionary<object, object> d)
                tostringed = d.MakeString();
            else if (result is string str)
                tostringed = str;
            else
                tostringed = result.MakeString();

            if (tostringed == "" || string.IsNullOrEmpty(tostringed) || tostringed.Length == 0)
                tostringed = " ";
            
            if (tostringed.Length > 1000)
                description += $"Here is a **[link]({await UploadToPastebinAsync(tostringed)})** to the result.";
            else
                description += $"```{tostringed}```";

            if (sb.ToString().Length > 0)
                description += $"\nConsole: \n```\n{sb}\n```";

            string footer = "";
            if (result is ICollection coll)
                footer += $"Collection has {coll.Count} members • ";
            else if (result is IReadOnlyCollection<object> colle)
                footer += $"Collection has {colle.Count} members • ";

            footer += $"Return type: {(result == null ? "null" : result.GetType().ToString())} • took {s.ElapsedTicks / 1000000d} ms to compile and {c.ElapsedTicks / 1000000d} ms to execute";


            var em = new EmbedBuilder()
                    .WithFooter(footer)
                    .WithDescription(description)
                    .WithColor(Color.Green);

            await msg.DeleteAsync();
            return em;
        }

        public async Task<EmbedBuilder> EvaluateLuaAsync(ShardedCommandContext context, string code)
        {
            Discord.Rest.RestUserMessage msg = await context.Channel.SendMessageAsync("Working...");

            code = code.Replace("“", "\"").Replace("‘", "\'").Replace("”", "\"").Replace("’", "\'").Trim('`');
            if (code.Length > 3 && code.Substring(0, 3) == "lua") code = code.Substring(3);
            
            var sb = new StringBuilder();
            var script = new MoonSharp.Interpreter.Script(CoreModules.Preset_SoftSandbox | CoreModules.Debug | CoreModules.LoadMethods);
            script.Options
                .DebugPrint = s => sb.Append(s + "\n");

            DynValue eval;
            Stopwatch c = Stopwatch.StartNew();
            try
            {
                eval = script.DoString(code);
            }
            catch (Exception e)
            {
                await msg.DeleteAsync();
                return await GenerateErrorAsync(code, e, "lua");
            }
            c.Stop();

            object result = null;
            switch(eval.Type)
            {
                case DataType.Number:
                    result = eval.Number;
                    break;
                case DataType.String:
                    result = eval.String;
                    break;
                case DataType.Table:
                    var tab = eval.Table;
                    var dict = new Dictionary<DynValue, DynValue>();
                    for (int i = 0; i < tab.Values.Count(); i++)
                        dict.Add(tab.Keys.ElementAt(i), tab.Values.ElementAt(i));
                    result = dict;
                    break;
                case DataType.Function:
                    result = eval.Function;
                    break;
                case DataType.Thread:
                    result = eval.Coroutine;
                    break;
                case DataType.Tuple:
                    result = eval.Tuple;
                    break;
                case DataType.Boolean:
                    result = eval.Boolean;
                    break;
                case DataType.UserData:
                    result = eval.UserData;
                    break;
            }

            string description;
            if (code.Length < 1000)
                 description = $"in: ```lua\n{code}```\nout: \n";
            else
                description = $"in: **[input]({await UploadToPastebinAsync(code)})**\nout: \n";
            string tostringed = (result == null) ? "nil" : result.ToString();

            if (result is ICollection r)
                tostringed = r.MakeString();
            else if (result is IReadOnlyCollection<object> x)
                tostringed = x.MakeString();
            else if (result is string str)
                tostringed = str;
            else
                tostringed = result.MakeString();

            if (tostringed == "" || string.IsNullOrEmpty(tostringed))
                tostringed = " ";

            if (tostringed.Length > 1000)
                description += $"Here is a **[link]({await UploadToPastebinAsync(tostringed)})** to the result.";
            else
                description += $"```{tostringed}```";

            

            string footer = "";
            if (result is ICollection coll)
                footer += $"Collection has {coll.Count} members • ";
            else if (result is IReadOnlyCollection<object> colle)
                footer += $"Collection has {colle.Count} members • ";


            if (sb.ToString().Length > 0)
            {
                if (sb.Length < 1000)
                    description += $"\nConsole: \n```\n{sb}\n```";
                else
                    description += $"\nConsole: \n[here]({await UploadToPastebinAsync(sb.ToString())})";
            }

            footer += $"Return type: {(result == null ? "nil" : _aliases[result.GetType()])} • took {c.ElapsedTicks / 1000000d} ms to execute";


            var em = new EmbedBuilder()
                    .WithFooter(footer)
                    .WithDescription(description)
                    .WithColor(Color.Green);

            await msg.DeleteAsync();
            return em;
        }

        private async Task<EmbedBuilder> GenerateErrorAsync(string code, Exception e, string lang)
        {
            bool doCodeBlockForIn = true;
            if (code.Length > 1000)
            {
                code = await UploadToPastebinAsync(code);
                doCodeBlockForIn = false;
            }
            string errorMsg;
            if (e.StackTrace != null)
                errorMsg = $"{e.Message}\n{e.StackTrace.Substring(0, e.StackTrace.IndexOf("---") + 1)}";
            else
                errorMsg = $"{e.Message}";
            bool doCodeBlockForOut = true;
            if (errorMsg.Length > 1000)
            {
                errorMsg = await UploadToPastebinAsync(errorMsg);
                doCodeBlockForOut = false;
            }

            string description;
            if (doCodeBlockForIn)
                description = $"in: ```{(lang == "cs" ? "cs" : "lua")}\n{code}```";
            else
                description = $"in:\n**[input]({code})**";

            if (doCodeBlockForOut)
                description += $"\n \nout: \n```{errorMsg}```";
            else
                description += $"\n \nout: \nHere is a **[link]({errorMsg})** to the error message.";

            var em = new EmbedBuilder()
                    .WithFooter($"{e.GetType()}")
                    .WithDescription(description)
                    .WithColor(Color.Red);
            return em;
        }
        private async Task<EmbedBuilder> GenerateErrorAsync(string code, IEnumerable<Diagnostic> compErrors, string lang)
        {
            var msg = new StringBuilder(compErrors.Count());
            foreach (var h in compErrors)
                msg.Append("• " + h.GetMessage() + "\n"); bool doCodeBlockForIn = true;
            if (code.Length > 1000)
            {
                code = await UploadToPastebinAsync(code);
                doCodeBlockForIn = false;
            }
            string errorMsg = msg.ToString();
            bool doCodeBlockForOut = true;
            if (errorMsg.Length > 1000)
            {
                errorMsg = await UploadToPastebinAsync(errorMsg);
                doCodeBlockForOut = false;
            }

            string description;
            if (doCodeBlockForIn)
                description = $"in: ```{(lang == "cs" ? "cs" : "lua")}\n{code}```";
            else
                description = $"in:\n**[input]({code})**";

            if (doCodeBlockForOut)
                description += $"\n \nout: \n```{errorMsg}```";
            else
                description += $"\n \nout: \nHere is a **[link]({errorMsg})** to the compilation errors.";

            var em = new EmbedBuilder()
                    .WithFooter(typeof(CompilationErrorException).ToString())
                    .WithDescription(description)
                    .WithColor(Color.Red);
            return em;
        }

        private static IEnumerable<Assembly> GetAssemblies()
        {
            var assemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies();
            foreach (var a in assemblies)
            {
                var s = Assembly.Load(a);
                yield return s;
            }
            yield return Assembly.GetEntryAssembly();
            yield return typeof(ILookup<string, string>).GetTypeInfo().Assembly;
        }

        public async Task<string> UploadToPastebinAsync(string stuffToUpload)
        {
            try
            {
                var sc = new FormUrlEncodedContent( new Dictionary<string, string> { { "input", stuffToUpload } } );
                sc.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                sc.Headers.Add("key", uploadKey);

                var request = await _hc.PostAsync("https://paste.jakedacatman.me/paste", sc);
                return await request.Content.ReadAsStringAsync();
            }
            catch (Exception e) //when (e.Message == "The remote server returned an error: (520) Origin Error.")
            {
                throw e;
            }
        }

        public async Task<string> DownloadAsStringAsync(string url)
        {
            try 
            {
                var response = await _hc.GetStringAsync(url);
                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public int RandomNumber(int min, int max) => _random.Next(min, max);
        public float RandomFloat(float max) => (float)_random.NextDouble() * max;
        public float RandomFloat(float max, float min) => (float)_random.NextDouble() * (max - min) + min;

        public int GenerateId()
        {
            int generated;
            do
            {
                generated = _random.Next();
            }
            while (ids.Contains(generated));
            ids.Add(generated);
            return generated;
        }

        public async Task<IMessage> GetNthMessageAsync(SocketTextChannel channel, int pos) => (await channel.GetMessagesAsync().FlattenAsync()).OrderByDescending(x => x.Timestamp).ToArray()[pos];

        public async Task<IMessage> GetPreviousMessageAsync(SocketTextChannel channel) => await GetNthMessageAsync(channel, 1);
    }
}