using System;
using System.Collections;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using Discord;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MoonSharp.Interpreter;

namespace donniebot.classes
{
    public static class IEnumerableExtensions
    {
        public static string MakeString(this Dictionary<DynValue, DynValue> t, int level = 0)
        {
            if (t == null || !(t as IEnumerable).Cast<object>().Any())
                return $"[\n{"  ".RepeatString(level)}]";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < t.Values.Count(); i++)
            {
                var key = t.Keys.ElementAt(i);
                var value = t.Values.ElementAt(i);

                var toAppend = "  ".RepeatString(level + 1);

                if (value.Type == DataType.Table)
                {
                    var tab = value.Table;
                    var dict = new Dictionary<DynValue, DynValue>();
                    for (int x = 0; x < tab.Values.Count(); x++)
                        dict.Add(tab.Keys.ElementAt(i), tab.Values.ElementAt(i));
                    toAppend += $"[{key}, \n{"  ".RepeatString(level + 2)}{dict.MakeString(level + 2)}\n{"  ".RepeatString(level + 1)}]";
                }
                else
                    toAppend += $"[{key}, {value.MakeString()}]";

                toAppend += ",\n";

                sb.Append(toAppend);
            }
            var str = sb.ToString();
            return $"[\n{str.Substring(0, str.Length - 2)}\n{"  ".RepeatString(level)}]";
        }
        public static string MakeString(this IEnumerable t, int level = 0)
        {
            if (t == null || !t.Cast<object>().Any())
                return $"[\n{"  ".RepeatString(level)}]";

            StringBuilder sb = new StringBuilder();
            foreach (var thing in t)
            {
                if (thing == null)
                    continue;

                var toAppend = "  ".RepeatString(level + 1);

                if (thing is ICollection h)
                    toAppend += h.MakeString(level + 1);
                else if (thing is IReadOnlyCollection<object> x)
                    toAppend += x.MakeString(level + 1);
                else
                    toAppend += thing.ToString();

                toAppend += ",\n";

                sb.Append(toAppend);
            }
            var str = sb.ToString();
            return $"[\n{str.Substring(0, str.Length - 2)}\n{"  ".RepeatString(level)}]";
        }

        public static bool ContainsObj(this IEnumerable<GuildImage> list, GuildImage obj)
        {
            foreach (var fart in list)
                if (GuildImage.IsEqual(fart, obj))
                    return true;
            return false;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> src)
        {
            var r = new Random();

            var e = src.ToArray();
            for (var i = e.Length - 1; i >= 0; i--)
            {
                var ind = r.Next(i + 1);
                yield return e[ind];
                e[ind] = e[i];
            }   
        }       
    }

    public static class ObjectExtensions
    {
        public static string MakeString(this DynValue d)
        {
            object result = null;
            switch(d.Type)
            {
                case DataType.Number:
                    result = d.Number;
                    break;
                case DataType.String:
                    result = d.String;
                    break;
                case DataType.Table:
                    var tab = d.Table;
                    var dict = new Dictionary<DynValue, DynValue>();
                    for (int i = 0; i < tab.Values.Count(); i++)
                        dict.Add(tab.Keys.ElementAt(i), tab.Values.ElementAt(i));
                    result = dict;
                    break;
                case DataType.Function:
                    result = d.Function;
                    break;
                case DataType.Thread:
                    result = d.Coroutine;
                    break;
                case DataType.Tuple:
                    result = d.Tuple;
                    break;
                case DataType.Boolean:
                    result = d.Boolean;
                    break;
                case DataType.UserData:
                    result = d.UserData;
                    break;
            }

            return result.MakeString();
        }

        private static readonly Dictionary<Type, string> Aliases = new Dictionary<Type, string>()
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
            { typeof(void), "void" }
        };

        public static string MakeString(this object t, bool doMethods = false, bool doValueType = false, bool doProperties = true, int level = 0)
        {
            if (t == null)
                return $" ";

            if (t.GetType().IsValueType && !doValueType) return t.ToString();

            StringBuilder sb = new StringBuilder();

            if (doProperties)
            {
                var properties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic).Where(x => x.GetIndexParameters().Length == 0);
                foreach (var thing in properties)
                {
                    if (thing == null)
                        continue;

                    var toAppend = "  ".RepeatString(level + 1);

                    var value = thing.GetValue(t);

                    if (value is ICollection h)
                        toAppend += $"{thing.Name}:\n  {h.MakeString(level + 1)}";
                    else if (value is IReadOnlyCollection<object> x)
                        toAppend += $"{thing.Name}:\n  {x.MakeString(level + 1)}";
                    else
                    {
                        string valueAsString = null;
                        if (value != null)
                        {
                            var tostring = value.ToString();
                            if (tostring.Length > 100)
                                valueAsString = tostring.Substring(0, 100) + "...";
                            else
                                valueAsString = tostring;
                        }
                        toAppend += $"{thing.Name}: {valueAsString ?? "null"}";
                    }

                    toAppend += ",\n";

                    sb.Append(toAppend);
                }
            }

            if (doMethods)
            {
                var methods = t.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                foreach (var thing in methods)
                {
                    if (thing == null)
                        continue;

                    var toAppend = "  ".RepeatString(level + 1);

                    if (thing.IsPublic) toAppend += $"public ";
                    if (thing.IsPrivate) toAppend += $"private ";
                    if (thing.IsFamily) toAppend += $"protected ";
                    if (thing.IsAssembly) toAppend += "internal ";
                    if (thing.IsStatic) toAppend += $"static ";
                    if (thing.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null) toAppend += $"async ";
                    if (thing.IsAbstract) toAppend += $"abstract ";
                    if (thing.IsOverride()) toAppend += "override ";
                    else if (thing.IsVirtual) toAppend += $"virtual ";

                    var type = thing.ReturnType;
                    toAppend += Aliases.ContainsKey(type) ? Aliases[type] + " " : type.ToString() + " ";

                    toAppend += thing.Name;

                    if (thing.IsGenericMethod || thing.IsGenericMethodDefinition) toAppend += "<T>";
                    toAppend += "(";
                    var parameters = thing.GetParameters();
                    foreach (var g in parameters)
                        toAppend += $"{(g.IsOut ? "out " : "")}{(Aliases.ContainsKey(g.ParameterType) ? Aliases[g.ParameterType] : g.ParameterType.ToString())} {g.Name}{(g.HasDefaultValue ? " = " + g.DefaultValue ?? "null" : "")}, ";

                    if (parameters.Length > 0) toAppend = toAppend.Substring(0, toAppend.Length - 2);

                    toAppend += ")";

                    toAppend += ",\n";

                    sb.Append(toAppend);
                }
            }

            var str = sb.ToString();
            if (str.Length == 0) return $"{t} (no properties/methods only)";
            return $"[\n{str.Substring(0, str.Length - 2)}\n{"  ".RepeatString(level)}]";
        }

        public static IEnumerable<object> Repeat(this object o, int amount) => Enumerable.Repeat(o, amount);
        public static string RepeatString(this string o, int amount) => string.Join(string.Empty, o.Repeat(amount));
    }

    public static class MethodInfoExtensions
    {
        public static bool IsOverride(this MethodInfo m) => m.GetBaseDefinition().DeclaringType != m.DeclaringType;
    }

    public static class StringExtensions
    {
        public static bool WillExit(this string s, out string message)
        {
            if (s.Contains("Environment.Exit"))
            {
                message = "This code calls Environment.Exit.";
                return true;
            }
            message = "This code will not exit.";
            return false;
        }
    }

    public static class BigIntegerExtensions
    {
        public static BigInteger Factorial(this int i)
        {
            if (i < 0) return (BigInteger)double.NaN;

            BigInteger h = 1;
            for (BigInteger q = 1; q <= i; q++)
                h *= q;

            return h;
        }
    }
}