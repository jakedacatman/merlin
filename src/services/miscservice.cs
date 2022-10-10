using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using merlin.classes;
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
using Fergun.Interactive;
using Fergun.Interactive.Pagination;

#pragma warning disable CA2200 //Re-throwing caught exception changes stack information

namespace merlin.services
{
    public class MiscService
    {
        private readonly DiscordShardedClient _client;
        private IServiceProvider _services;
        private readonly NetService _net;
        private readonly Random _random;
        private readonly RandomService _rand;
        private readonly InteractiveService _inter;

        private readonly Dictionary<Type, string> luaTypeAliases = new Dictionary<Type, string>()
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
            { typeof(TimeSpan?), "timespan" },
        };

        private readonly Dictionary<Type, string> preconditionAliases = new Dictionary<Type, string>()
        {
            { typeof(RequireOwnerAttribute), "owner-only" },
            { typeof(RequireUserPermissionAttribute), "user requires perms" },
            { typeof(RequireNsfwAttribute), "requires nsfw channel" },
            { typeof(RequireBotPermissionAttribute), "bot requires perms" },
            { typeof(RequireContextAttribute), "must be invoked in a guild or dm" }
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

        public MiscService(DiscordShardedClient client, IServiceProvider services, NetService net, Random random, RandomService rand, InteractiveService inter)
        {
            _client = client;
            _services = services;
            _random = random;
            _net = net;
            _rand = rand;
            _inter = inter;
        }

        private readonly PhraseCollection errorPhrases = PhraseCollection.Load("phrases.txt", "phrases");

        public async Task<EmbedBuilder> GenerateErrorMessageAsync(Exception e)
        {
            var description = "";
            if (e is Discord.Net.HttpException ex && ex.DiscordCode == DiscordErrorCode.RequestEntityTooLarge)
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
                    description += $"a [link]({await _net.UploadToPastebinAsync(message)}) to its message.";

                string trace = e.StackTrace;
                if (inex is not null) trace += $"\ninner exception: {inex.StackTrace}";

                var info = $"{e.Message}\n\n{trace}";

                description += $"\nHere is a [link]({await _net.UploadToPastebinAsync(info)}) to the full information on this exception.";

                description += $"\n\nPlease be sure to DM jakedacatman#6121 on [Discord](https://discord.com) with this link or file an issue on the [GitHub page](https://github.com/jakedacatman/merlin).";
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

            var console = new StringBuilder();

            var globals = new Globals
            {
                _client = _client,
                _context = context,
                _guild = context.Guild,
                _channel = context.Channel,
                _user = context.User as SocketGuildUser,
                _services = _services,
                _message = context.Message,
                Console = new FakeConsole(console),
                _db = _services.GetService<DbService>(),
                _misc = this,
                Random = _random,
                _commands = _services.GetService<CommandService>(),
                _img = _services.GetService<ImageService>(),
                _net = _services.GetService<NetService>(),
                _rand = _services.GetService<RandomService>()
            };
            globals._globals = globals;

            Microsoft.CodeAnalysis.Scripting.ScriptOptions options = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
                .AddReferences(assemblies)
                .WithAllowUnsafe(true)
                .WithLanguageVersion(LanguageVersion.Preview);

            var imports = assemblies
                .Select(x => x.GetTypes()
                .Select(y => y.Namespace))
                .SelectMany(x => x)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct();
            
            try
            {
                options = options.AddImports(imports);
            }
            catch (ReflectionTypeLoadException e)
            {
                options = options
                    .AddImports(e.Types
                        .Where(x => x is not null)
                        .Select(y => y.Namespace)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                );
            }

            globals.Imports = options.Imports.ToArray();

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
                 description = $"in: ```cs\n{code}```\n";
            else
                description = $"in: **[input]({await _net.UploadToPastebinAsync(code)})**\n";

            string tostringed = result == null ? "null" : PrettyPrint(result);

            if (string.IsNullOrWhiteSpace(tostringed) || tostringed.Length == 0)
                tostringed = " ";
            
            if (tostringed.Length > 1000)
                description += $"Here is a **[link]({await _net.UploadToPastebinAsync(tostringed)})** to the result.";
            else
                description += $"out: \n```{tostringed}```";

            if (console.ToString().Length > 0)
                description += $"\nConsole: \n```\n{console}\n```";

            string footer = "";
            if (result is IEnumerable<object> x)
                footer += $"Collection has {x.Count()} members • ";

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
            
            var console = new StringBuilder();
            var script = new MoonSharp.Interpreter.Script(CoreModules.Preset_HardSandbox);
            script.Options
                .DebugPrint = s => console.Append(s + "\n");

            #pragma warning disable VSTHRD101 //Avoid using async lambda for a void returning delegate type, because any exceptions not handled by the delegate will crash the process. (i handle it lol)
            script.Globals["XD"] = (Action)(async () => 
            {
                try
                {
                    await channel.SendMessageAsync("XD");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            script.Globals["_G"] = "_G";
            script.Globals["sendMessage"] = (Action<string>)(async (string text) => 
            {
                try
                {
                    if (text.Length > 2000)
                        text = $"Message too long; here is a link to it: {await _net.UploadToPastebinAsync(text)}";

                    await channel.SendMessageAsync(text);
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
            
            if (eval.Type == DataType.Table)
            {
                var tab = eval.Table;
                var dict = new Dictionary<object, object>();
                for (int i = 0; i < tab.Values.Count(); i++)
                    dict.Add(tab.Keys.ElementAt(i), tab.Values.ElementAt(i));
                result = dict;
            }
            else result = eval.ToObject();

            string description;
            if (code.Length < 1000)
                 description = $"in: ```lua\n{code}```\nout: \n";
            else
                description = $"in: **[input]({await _net.UploadToPastebinAsync(code)})**\nout: \n";

            string tostringed = (result == null) ? "nil" : PrettyPrint(result);

            if (string.IsNullOrEmpty(tostringed))
                tostringed = "\"\"";

            if (tostringed.Length > 1000)
                description += $"Here is a **[link]({await _net.UploadToPastebinAsync(tostringed)})** to the result.";
            else
                description += $"```{tostringed}```";

            string footer = "";
            if (result is IEnumerable<object> x)
                footer += $"Collection has {x.Count()} members • ";

            if (console.ToString().Length > 0)
            {
                if (console.Length < 1000)
                    description += $"\nConsole: \n```\n{console}\n```";
                else
                    description += $"\nConsole: \n[here]({await _net.UploadToPastebinAsync(console.ToString())})";
            }

            footer += $"Return type: {(result == null ? "nil" : luaTypeAliases[result.GetType()])} • took {c.ElapsedTicks / 1000000d} ms to execute";

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

        public string PrettyPrintDV(DynValue d, int level = 0)
        {
            object result = null;

            if (d.Type == DataType.Table)
            {
                var tab = d.Table;
                var dict = new Dictionary<object, object>();
                for (int i = 0; i < tab.Values.Count(); i++)
                    dict.Add(tab.Keys.ElementAt(i), tab.Values.ElementAt(i));
                result = dict;
            }
            else result = d.ToObject();

            return PrettyPrint(result, level);
        }
        public string PrettyPrintDict<T, U>(Dictionary<T, U> dict, int level = 0)
        {
            if (dict == null)
                return "null";

            StringBuilder sb = new StringBuilder();
            
            int nextLevel = level + 1;
            
            for (int i = 0; i < dict.Keys.Count; i++)
            {
                var key = dict.Keys.ElementAt(i);
                var value = dict.Values.ElementAt(i);
                    
                sb.Append($"{"  ".RepeatString(nextLevel)}[{(key is string ? $"\"{key}\"" : key.ToString())}, {PrettyPrint(value, nextLevel)}\n{"  ".RepeatString(nextLevel)}],\n");
            }
            
            if (!dict.Cast<object>().Any())
            {
                if (level > 0)
                    return $"[\n{"  ".RepeatString(nextLevel)}]";
                else
                    return $"[\n{"  ".RepeatString(level)}]";
            }
            
            var str = sb.ToString();
            return $"[\n{str.Substring(0, str.Length - 2)}\n{"  ".RepeatString(level)}]";
        }
        public string PrettyPrintEnumerable(IEnumerable t, int level = 0)
        {
            if (t == null)
                return "null";
            
            if (!t.Cast<object>().Any())
                return $"[\n{"  ".RepeatString(level)}]";

            StringBuilder sb = new StringBuilder();

            foreach (var thing in t)
            {
                var val = thing is null ? "null" : PrettyPrint(thing, level + 1);
                sb.Append($"{"  ".RepeatString(level + 1)}{val},\n");
            }

            var str = sb.ToString();
            return $"[\n{str.Substring(0, str.Length - 2)}\n{"  ".RepeatString(level)}]";
        }
        private string PrettyPrint(object t, int level = 0)
        {
            string str = " ";
            try
            {
                if (t == null)
                    return "null";
                if (t is string x)
                {
                    if (string.IsNullOrEmpty(x))
                        return "\"\"";
                    else
                        return $"\"{x}\"";
                }
 
                if (t.GetType().IsValueType) return t.ToString();

                if (t is IDictionary di)
                    return PrettyPrintDict(
                        CastDict(di)
                            .ToDictionary(
                                x => (object)x.Key, 
                                x => (object)x.Value
                            ), 
                        level);
                else if (t is IEnumerable h)
                    return PrettyPrintEnumerable(h, level);
                else if (t is DynValue d)
                    return PrettyPrintDV(d, level);

                StringBuilder sb = new StringBuilder();
                
                var properties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic).Where(x => !x.GetIndexParameters().Any());
                foreach (var thing in properties)
                {
                    var toAppend = "  ".RepeatString(level + 1);
                    if (thing == null)
                    {
                        sb.Append($"{thing.Name}: null");
                    }
                    else
                    {
                        object value;
                        if ((thing.Name.Contains("Exit") || thing.Name.Contains("Start") || thing.Name.Contains("Standard")) && t is System.Diagnostics.Process)
                            value = "(none)";
                        else
                            value = thing.GetValue(t);
                        
                        toAppend += $"{thing.Name}: ";

                        if (t is Closure && thing.Name == "ClosureContext") //the type is internal and i can't help that plus it is the lua _ENV table (which contains itself)
                            toAppend += "_ENV";
                        else if (value is IDictionary dict)
                            toAppend += PrettyPrintDict(
                                CastDict(dict)
                                    .ToDictionary(
                                        x => (object)x.Key, 
                                        x => (object)x.Value
                                    ), 
                                level + 1
                            );
                        else if (value is IEnumerable h)
                            toAppend += $"\n{PrettyPrintEnumerable(h, level + 1)}";
                        else if (value is DynValue d)
                            toAppend += PrettyPrintDV(d, level + 1);
                        else
                        {
                            string valueAsString = null;
                            if (value != null)
                            {
                                if (value is string s)
                                {
                                    if (string.IsNullOrEmpty(s))
                                        valueAsString = "\"\"";
                                    else
                                        valueAsString = $"\"{s}\"";
                                }
                                else valueAsString = value.ToString();
                            }

                            toAppend += valueAsString ?? "null";
                        }

                        toAppend += ",\n";
                        sb.Append(toAppend);
                    }
                }

                str = sb.ToString();
                if (str.Length == 0) return $"{t} - no properties (methods only)";
                return $"[\n{str.Substring(0, str.Length - 2)}\n{"  ".RepeatString(level)}]";               
            }
            catch
            {
                return t.ToString();
            }
        }

        private IEnumerable<DictionaryEntry> CastDict(IDictionary d)
        {
            foreach (DictionaryEntry e in d)
                yield return e;
        }

        public IEnumerable<string> GetCommands(IEnumerable<CommandInfo> commands)
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
                        txt = attr.GuildPermission.HasValue ? $" ({attr.GuildPermission.Value.ToString()})" : txt;
                    else if (p is RequireBotPermissionAttribute attr2)
                        txt = attr2.GuildPermission.HasValue ? $" ({attr2.GuildPermission.Value.ToString()})" : txt;

                    preconditions += $"{preconditionAliases[p.GetType()]}{txt}\n";
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
                    .WithUsers(user)
                    .WithInputType(InputType.Buttons)
                    .WithOptions(new Dictionary<IEmote, PaginatorAction>
                    {   
                        { new Emoji("◀️"), PaginatorAction.Backward },
                        { new Emoji("▶️"), PaginatorAction.Forward },
                        { new Emoji("⏮️"), PaginatorAction.SkipToStart },
                        { new Emoji("⏭️"), PaginatorAction.SkipToEnd },
                        { new Emoji("🛑"), PaginatorAction.Exit }
                    })
                    .WithPages(pages);
            else
                paginator = new StaticPaginatorBuilder()
                    .WithUsers(user)
                    .WithInputType(InputType.Buttons)
                    .WithOptions(new Dictionary<IEmote, PaginatorAction> { { new Emoji("🛑"), PaginatorAction.Exit } })
                    .WithPages(pages);
            
            return paginator;
        }

        public async Task<InteractiveMessageResult> SendPaginatorAsync(SocketUser user, IEnumerable<PageBuilder> pages, ISocketMessageChannel channel)
        {
            return await _inter.SendPaginatorAsync(new StaticPaginatorBuilder()
                .WithUsers(user)
                .WithInputType(InputType.Buttons)
                .WithFooter(PaginatorFooter.PageNumber)
                .WithOptions(new Dictionary<IEmote, PaginatorAction>
                {   
                    { new Emoji("◀️"), PaginatorAction.Backward },
                    { new Emoji("▶️"), PaginatorAction.Forward },
                    { new Emoji("⏮️"), PaginatorAction.SkipToStart },
                    { new Emoji("⏭️"), PaginatorAction.SkipToEnd },
                    { new Emoji("🛑"), PaginatorAction.Exit }
                })
                .WithPages(pages)
                .Build(), channel);
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
        public string PrettyFormatBits(long bits, int place)
        {
            double bd = bits; 
            var sb = new StringBuilder();
            long divisor = 1 << 10;

            for (int i = 0; i < prefixes.Count; i++)
            {
                if (bd < 1024d)
                {
                    sb.Append($"{Math.Round(bd, place)} {prefixes[i]}bit{(Math.Round(bd, place) == 1 ? "" : "s")}");
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
