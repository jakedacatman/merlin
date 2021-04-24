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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Interactivity;
using Interactivity.Pagination;

#pragma warning disable CA2200 //Re-throwing caught exception changes stack information

namespace donniebot.services
{
    public class MiscService
    {
        private readonly DiscordShardedClient _client;
        private IServiceProvider _services;
        private readonly NetService _net;
        private readonly Random _random;
        private readonly RandomService _rand;

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

        private readonly Dictionary<Type, string> typeAliases = new Dictionary<Type, string>()
        {
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(void), "void" },
            { typeof(SocketGuildUser), "user" },
            { typeof(TimeSpan), "timespan" },
            { typeof(SocketRole), "role" },
        };

        private readonly Dictionary<Type, string> preconditionAliases = new Dictionary<Type, string>()
        {
            { typeof(RequireOwnerAttribute), "owner-only" },
            { typeof(RequireUserPermissionAttribute), "user requires perms" },
            { typeof(RequireNsfwAttribute), "requires nsfw channel" },
            { typeof(RequireBotPermissionAttribute), "bot requires perms" },
            { typeof(RequireContextAttribute), "must be invoked in a guild or dm" },
	    { typeof(RequireDjRoleAttribute), "requires dj role" }
        };

        private readonly List<string> prefixes = new List<string>
        {
            "",
            "Ki",
            "Mi",
            "Gi",
            "Ti",
            "Pi"
        };

        public MiscService(DiscordShardedClient client, IServiceProvider services, NetService net, Random random, RandomService rand)
        {
            _client = client;
            _services = services;
            _random = random;
            _net = net;
            _rand = rand;
        }

        private readonly PhraseCollection errorPhrases = PhraseCollection.Load("phrases.txt", "phrases");

        public async Task<EmbedBuilder> GenerateErrorMessageAsync(Exception e)
        {
            var description = "";
            if (e is Discord.Net.HttpException ex && ex.DiscordCode == 40005)
                description = "The resulting file was too large to upload to Discord.";
            else if (e is ImageException ie)
                description = ie.Message;
            else if (e is VideoException ve)
                description = ve.Message;
            else if (e is HttpRequestException he)
                description = $"An exception occurred when making an HTTP request: {he.Message}";
            else if (e is SixLabors.ImageSharp.UnknownImageFormatException)
                description = "The image format was not valid.";
            else
            {
                description = "This command has thrown an exception. Here is ";

                var inex = e.InnerException;
                string message = e.Message;
                if (inex != null) message += $"\n*(inner: {inex.Message})*";

                if (message.Length < 500)
                    description += $"its message:\n\n**{message.Replace("`", @"\`")}**";
                else
                    description += $"a [link]({await _net.UploadToPastebinAsync(message)} to its message.";

                description += "\n\nStack trace:\n";

                string trace = e.StackTrace;
                if (inex != null) trace += $"\ninner: {inex.StackTrace}";

                if (trace.Length < 500)
                    description += $"```{trace.Replace("`", @"\`")}```";
                else
                    description += $"[here]({await _net.UploadToPastebinAsync(trace)})";

                description += $"\n\nPlease be sure to DM jakedacatman#6121 on [Discord](https://discord.com) or file an issue on the [GitHub page](https://github.com/jakedacatman/donniebot) with this information.";
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(_rand.RandomColor())
                .WithCurrentTimestamp()
                .WithFooter(e.GetType().ToString())
                .WithDescription(description)
                .WithTitle(errorPhrases[_random.Next(errorPhrases.Count)]);

            return embed;
            
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
                _net = _services.GetService<NetService>(),
                _rand = _services.GetService<RandomService>(),
                _audio = _services.GetService<AudioService>()
            };
            globals._globals = globals;

            Microsoft.CodeAnalysis.Scripting.ScriptOptions options = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
                .AddReferences(assemblies)
                .WithAllowUnsafe(true)
                .WithLanguageVersion(LanguageVersion.Preview);
            
            try
            {
                options = options
                    .AddImports(assemblies
                        .Select(x => x.GetTypes()
                        .Select(y => y.Namespace))
                    .SelectMany(x => x)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                );
            }
            catch (ReflectionTypeLoadException e)
            {
                options = options
                    .AddImports(e.Types
                        .Where(x => x is not null)
                        .Where(x => x != typeof(UkooLabs.SVGSharpie.ImageSharp.Shapes.ArcLineSegemnt))
                        .Select(y => y.Namespace)
                        .Where(x => !string.IsNullOrWhiteSpace(x)
                    )
                );
            }

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
                description = $"in: **[input]({await _net.UploadToPastebinAsync(code)})**\nout: \n";
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
                description += $"Here is a **[link]({await _net.UploadToPastebinAsync(tostringed)})** to the result.";
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

        public async Task<EmbedBuilder> EvaluateLuaAsync(SocketTextChannel channel, string code)
        {
            Discord.Rest.RestUserMessage msg = await channel.SendMessageAsync("Working...");

            code = code.Replace("“", "\"").Replace("‘", "\'").Replace("”", "\"").Replace("’", "\'").Trim('`');
            if (code.Length > 3 && code.Substring(0, 3) == "lua") code = code.Substring(3);
            
            var sb = new StringBuilder();
            var script = new MoonSharp.Interpreter.Script(CoreModules.Preset_SoftSandbox);
            script.Options
                .DebugPrint = s => sb.Append(s + "\n");

            #pragma warning disable VSTHRD101 //Avoid using async lambda for a void returning delegate type, because any exceptions not handled by the delegate will crash the process. (i handle it lol)
            script.Globals["XD"] = (Action)(async () => 
            {
                try
                {
                    await channel.SendMessageAsync("XD");
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            script.Globals["sendMessage"] = (Action<string>)(async (string text) => 
            {
                try
                {
                    if (text.Length > 1000)
                        text = $"Message too long; here is a link to it: {await _net.UploadToPastebinAsync(text)}";

                    await channel.SendMessageAsync(text);
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            #pragma warning restore VSTHRD101

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
                description = $"in: **[input]({await _net.UploadToPastebinAsync(code)})**\nout: \n";
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
                description += $"Here is a **[link]({await _net.UploadToPastebinAsync(tostringed)})** to the result.";
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
                    description += $"\nConsole: \n[here]({await _net.UploadToPastebinAsync(sb.ToString())})";
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
                code = await _net.UploadToPastebinAsync(code);
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
                errorMsg = await _net.UploadToPastebinAsync(errorMsg);
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
                code = await _net.UploadToPastebinAsync(code);
                doCodeBlockForIn = false;
            }
            string errorMsg = msg.ToString();
            bool doCodeBlockForOut = true;
            if (errorMsg.Length > 1000)
            {
                errorMsg = await _net.UploadToPastebinAsync(errorMsg);
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
                if (!string.IsNullOrEmpty(s.Location) && s.GetName().Name != "UkooLabs.SVGSharpie.ImageSharp") //this causes issues
                    yield return s;
            }

            var entry = Assembly.GetEntryAssembly();
            if (!string.IsNullOrEmpty(entry?.Location) || !string.IsNullOrEmpty(System.AppContext.BaseDirectory))
                yield return entry;

            var il = typeof(ILookup<string, string>).GetTypeInfo().Assembly;
            if (!string.IsNullOrEmpty(il?.Location) || !string.IsNullOrEmpty(System.AppContext.BaseDirectory))
                yield return il;
        }

        public  IEnumerable<string> GetCommands(IEnumerable<CommandInfo> commands)
        {
            foreach (var cmd in commands)
                yield return ((string.IsNullOrEmpty(cmd.Module.Group) ? "" : $"{cmd.Module.Group} ") + cmd.Name).TrimEnd(' ');
        }

        public StaticPaginatorBuilder GenerateCommandInfo(IEnumerable<CommandInfo> commands, SocketGuildUser user)
        {
            var pages = new List<PageBuilder>();

            foreach (var cmd in commands)
            {
                string preconditions = null;
                if (cmd.Preconditions.Any())
                foreach (PreconditionAttribute p in cmd.Preconditions)
                {
                    var txt = "";
                    if (p is RequireUserPermissionAttribute attr)
                        txt = attr.GuildPermission.HasValue ? attr.GuildPermission.Value.ToString() : txt;
                    else if (p is RequireBotPermissionAttribute attr2)
                        txt = attr2.GuildPermission.HasValue ? attr2.GuildPermission.Value.ToString() : txt;

                    preconditions += $"{preconditionAliases[p.GetType()]} ({txt})\n";
                }

                var name = ((string.IsNullOrEmpty(cmd.Module.Group) ? "" : $"{cmd.Module.Group} ") + cmd.Name).TrimEnd(' ');

                var fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder().WithName("Name").WithValue(name).WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Category").WithValue(cmd.Module.Name ?? "(none)").WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Aliases").WithValue(cmd.Aliases.Count > 1 ? string.Join(", ", cmd.Aliases.Where(x => x != cmd.Name)) : "(none)").WithIsInline(true),
                    new EmbedFieldBuilder().WithName("Summary").WithValue(cmd.Summary ?? "(none)").WithIsInline(false),
                    new EmbedFieldBuilder().WithName("Preconditions").WithValue(preconditions ?? "(none)").WithIsInline(false),
                };

                var parameters = new List<string>();
                foreach (Discord.Commands.ParameterInfo param in cmd.Parameters)
                    parameters.Add($"{param} ({typeAliases[param.Type]}{(param.DefaultValue != null ? ", default = " + param.DefaultValue.ToString() : "")}): {param.Summary}");
                
                fields.Add(new EmbedFieldBuilder().WithName("Parameters").WithValue(parameters.Any() ? string.Join("\n", parameters) : "(none)").WithIsInline(false));
            
                pages.Add(new PageBuilder()
                    .WithColor(_rand.RandomColor())
                    .WithTitle($"Information for {name}")
                    .WithFields(fields)
                );
            }

            StaticPaginatorBuilder paginator;
            
            if (pages.Count > 1)
                paginator = new StaticPaginatorBuilder()
                    .WithDeletion(DeletionOptions.AfterCapturedContext)
                    .WithUsers(user)
                    .WithDefaultEmotes()
                    .WithPages(pages);
            else
                paginator = new StaticPaginatorBuilder()
                    .WithDeletion(DeletionOptions.AfterCapturedContext)
                    .WithUsers(user)
                    .WithEmotes(new Dictionary<IEmote, PaginatorAction> { { new Emoji("🛑"), PaginatorAction.Exit } })
                    .WithPages(pages);
            
            return paginator;
        }

        public string PrettyFormat(long bytes, int place)
        {
            var bd = (double) bytes; 
            var sb = new StringBuilder();
            long divisor = 1 << 10;

            for (int i = 0; i < prefixes.Count; i++)
            {
                if (bd < 1024d)
                {
                    sb.Append($"{Math.Round(bd, place)} {prefixes[i]}B");
                    break;
                }

                bd /= divisor;
            }

            return sb.ToString();
        }

        public async Task<IMessage> GetNthMessageAsync(SocketTextChannel channel, int pos) => (await channel.GetMessagesAsync().FlattenAsync()).OrderByDescending(x => x.Timestamp).ToArray()[pos];

        public async Task<IMessage> GetPreviousMessageAsync(SocketTextChannel channel) => await GetNthMessageAsync(channel, 1);
    }
}
#pragma warning restore CA2200 //Re-throwing caught exception changes stack information
