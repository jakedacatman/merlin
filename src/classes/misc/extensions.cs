using System;
using System.Collections;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MoonSharp.Interpreter;

namespace merlin.classes
{
    public static class IEnumerableExtensions
    {
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
        public static IEnumerable<object> Repeat(this object o, int amount) => Enumerable.Repeat(o, amount);
        public static string RepeatString(this string o, int amount) => string.Join(string.Empty, o.Repeat(amount));
    }

    public static class MethodInfoExtensions
    {
        public static bool IsOverride(this MethodInfo m) => m.GetBaseDefinition().DeclaringType != m.DeclaringType;
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