using Microsoft.Extensions.Configuration;
using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Neo
{
    public static class Helper
    {
        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        #region Avoid Linq

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool MemoryEquals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
        {
            if (x == y) return true;
            int len = x.Length;
            if (len != y.Length) return false;
            if (len == 0) return true;
            fixed (byte* xp = x, yp = y)
            {
                long* xlp = (long*)xp, ylp = (long*)yp;
                for (; len >= 8; len -= 8)
                {
                    if (*xlp != *ylp) return false;
                    xlp++;
                    ylp++;
                }
                byte* xbp = (byte*)xlp, ybp = (byte*)ylp;
                for (; len > 0; len--)
                {
                    if (*xbp != *ybp) return false;
                    xbp++;
                    ybp++;
                }
            }
            return true;
        }

        public static T[] Reverse<T>(this T[] origin)
        {
            var ret = new T[origin.Length];
            for (int x = 0, m = ret.Length - 1; x <= m; x++)
            {
                ret[x] = origin[m - x];
            }
            return ret;
        }

        public static byte[] Skip(this byte[] a, int index)
        {
            var length = a.Length - index;

            if (length < 0) throw new ArgumentException(nameof(a));

            var ret = new byte[length];
            Buffer.BlockCopy(a, index, ret, 0, length);
            return ret;
        }

        public static byte[] Take(this byte[] a, int count) => Take(a, 0, count);

        public static byte[] Take(this byte[] a, int index, int count)
        {
            if (count < 0) throw new ArgumentException(nameof(count));

            var ret = new byte[count];
            Buffer.BlockCopy(a, index, ret, 0, count);
            return ret;
        }

        public static byte[] Concat(this byte[] a, byte[] b)
        {
            var ret = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, ret, 0, a.Length);
            Buffer.BlockCopy(b, 0, ret, a.Length, b.Length);
            return ret;
        }

        #endregion

        private static int BitLen(int w)
        {
            return (w < 1 << 15 ? (w < 1 << 7
                ? (w < 1 << 3 ? (w < 1 << 1
                ? (w < 1 << 0 ? (w < 0 ? 32 : 0) : 1)
                : (w < 1 << 2 ? 2 : 3)) : (w < 1 << 5
                ? (w < 1 << 4 ? 4 : 5)
                : (w < 1 << 6 ? 6 : 7)))
                : (w < 1 << 11
                ? (w < 1 << 9 ? (w < 1 << 8 ? 8 : 9) : (w < 1 << 10 ? 10 : 11))
                : (w < 1 << 13 ? (w < 1 << 12 ? 12 : 13) : (w < 1 << 14 ? 14 : 15)))) : (w < 1 << 23 ? (w < 1 << 19
                ? (w < 1 << 17 ? (w < 1 << 16 ? 16 : 17) : (w < 1 << 18 ? 18 : 19))
                : (w < 1 << 21 ? (w < 1 << 20 ? 20 : 21) : (w < 1 << 22 ? 22 : 23))) : (w < 1 << 27
                ? (w < 1 << 25 ? (w < 1 << 24 ? 24 : 25) : (w < 1 << 26 ? 26 : 27))
                : (w < 1 << 29 ? (w < 1 << 28 ? 28 : 29) : (w < 1 << 30 ? 30 : 31)))));
        }

        internal static int GetBitLength(this BigInteger i)
        {
            byte[] b = i.ToByteArray();
            return (b.Length - 1) * 8 + BitLen(i.Sign > 0 ? b[b.Length - 1] : 255 - b[b.Length - 1]);
        }

        internal static int GetLowestSetBit(this BigInteger i)
        {
            if (i.Sign == 0)
                return -1;
            byte[] b = i.ToByteArray();
            int w = 0;
            while (b[w] == 0)
                w++;
            for (int x = 0; x < 8; x++)
                if ((b[w] & 1 << x) > 0)
                    return x + w * 8;
            throw new Exception();
        }

        internal static void Remove<T>(this HashSet<T> set, ISet<T> other)
        {
            if (set.Count > other.Count)
            {
                set.ExceptWith(other);
            }
            else
            {
                set.RemoveWhere(u => other.Contains(u));
            }
        }

        internal static void Remove<T>(this HashSet<T> set, FIFOSet<T> other)
            where T : IEquatable<T>
        {
            if (set.Count > other.Count)
            {
                set.ExceptWith(other);
            }
            else
            {
                set.RemoveWhere(u => other.Contains(u));
            }
        }

        internal static void Remove<T, V>(this HashSet<T> set, IReadOnlyDictionary<T, V> other)
        {
            if (set.Count > other.Count)
            {
                set.ExceptWith(other.Keys);
            }
            else
            {
                set.RemoveWhere(u => other.ContainsKey(u));
            }
        }

        internal static string GetVersion(this Assembly assembly)
        {
            CustomAttributeData attribute = assembly.CustomAttributes.FirstOrDefault(p => p.AttributeType == typeof(AssemblyInformationalVersionAttribute));
            if (attribute == null) return assembly.GetName().Version.ToString(3);
            return (string)attribute.ConstructorArguments[0].Value;
        }

        public static byte[] HexToBytes(this string value)
        {
            if (value == null || value.Length == 0)
                return new byte[0];
            if (value.Length % 2 == 1)
                throw new FormatException();
            byte[] result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return result;
        }

        internal static BigInteger Mod(this BigInteger x, BigInteger y)
        {
            x %= y;
            if (x.Sign < 0)
                x += y;
            return x;
        }

        internal static BigInteger ModInverse(this BigInteger a, BigInteger n)
        {
            BigInteger i = n, v = 0, d = 1;
            while (a > 0)
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }
            v %= n;
            if (v < 0) v = (v + n) % n;
            return v;
        }

        internal static BigInteger NextBigInteger(this Random rand, int sizeInBits)
        {
            if (sizeInBits < 0)
                throw new ArgumentException("sizeInBits must be non-negative");
            if (sizeInBits == 0)
                return 0;
            byte[] b = new byte[sizeInBits / 8 + 1];
            rand.NextBytes(b);
            if (sizeInBits % 8 == 0)
                b[b.Length - 1] = 0;
            else
                b[b.Length - 1] &= (byte)((1 << sizeInBits % 8) - 1);
            return new BigInteger(b);
        }

        internal static BigInteger NextBigInteger(this RandomNumberGenerator rng, int sizeInBits)
        {
            if (sizeInBits < 0)
                throw new ArgumentException("sizeInBits must be non-negative");
            if (sizeInBits == 0)
                return 0;
            byte[] b = new byte[sizeInBits / 8 + 1];
            rng.GetBytes(b);
            if (sizeInBits % 8 == 0)
                b[b.Length - 1] = 0;
            else
                b[b.Length - 1] &= (byte)((1 << sizeInBits % 8) - 1);
            return new BigInteger(b);
        }

        public static BigInteger Sum(this IEnumerable<BigInteger> source)
        {
            var sum = BigInteger.Zero;
            foreach (var bi in source) sum += bi;
            return sum;
        }

        internal static bool TestBit(this BigInteger i, int index)
        {
            return (i & (BigInteger.One << index)) > BigInteger.Zero;
        }


        public static string ToHexString(this IEnumerable<byte> value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in value)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }

        public static string ToHexString(this ReadOnlySpan<byte> value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in value)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static int ToInt32(this byte[] value, int startIndex)
        {
            fixed (byte* pbyte = &value[startIndex])
            {
                return *((int*)pbyte);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static long ToInt64(this byte[] value, int startIndex)
        {
            fixed (byte* pbyte = &value[startIndex])
            {
                return *((long*)pbyte);
            }
        }

        public static uint ToTimestamp(this DateTime time)
        {
            return (uint)(time.ToUniversalTime() - unixEpoch).TotalSeconds;
        }

        public static ulong ToTimestampMS(this DateTime time)
        {
            return (ulong)(time.ToUniversalTime() - unixEpoch).TotalMilliseconds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static ushort ToUInt16(this byte[] value, int startIndex)
        {
            fixed (byte* pbyte = &value[startIndex])
            {
                return *((ushort*)pbyte);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static uint ToUInt32(this byte[] value, int startIndex)
        {
            fixed (byte* pbyte = &value[startIndex])
            {
                return *((uint*)pbyte);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static ulong ToUInt64(this byte[] value, int startIndex)
        {
            fixed (byte* pbyte = &value[startIndex])
            {
                return *((ulong*)pbyte);
            }
        }

        internal static IPAddress Unmap(this IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            return address;
        }

        internal static IPEndPoint Unmap(this IPEndPoint endPoint)
        {
            if (!endPoint.Address.IsIPv4MappedToIPv6)
                return endPoint;
            return new IPEndPoint(endPoint.Address.Unmap(), endPoint.Port);
        }

        internal static BigInteger WeightedAverage<T>(this IEnumerable<T> source, Func<T, BigInteger> valueSelector, Func<T, BigInteger> weightSelector)
        {
            BigInteger sum_weight = BigInteger.Zero;
            BigInteger sum_value = BigInteger.Zero;
            foreach (T item in source)
            {
                BigInteger weight = weightSelector(item);
                sum_weight += weight;
                sum_value += valueSelector(item) * weight;
            }
            if (sum_value == BigInteger.Zero) return BigInteger.Zero;
            return sum_value / sum_weight;
        }

        internal static IEnumerable<TResult> WeightedFilter<T, TResult>(this IList<T> source, double start, double end, Func<T, BigInteger> weightSelector, Func<T, BigInteger, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (start < 0 || start > 1) throw new ArgumentOutOfRangeException(nameof(start));
            if (end < start || start + end > 1) throw new ArgumentOutOfRangeException(nameof(end));
            if (weightSelector == null) throw new ArgumentNullException(nameof(weightSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            if (source.Count == 0 || start == end) yield break;
            double amount = (double)source.Select(weightSelector).Sum();
            BigInteger sum = 0;
            double current = 0;
            foreach (T item in source)
            {
                if (current >= end) break;
                BigInteger weight = weightSelector(item);
                sum += weight;
                double old = current;
                current = (double)sum / amount;
                if (current <= start) continue;
                if (old < start)
                {
                    if (current > end)
                    {
                        weight = (long)((end - start) * amount);
                    }
                    else
                    {
                        weight = (long)((current - start) * amount);
                    }
                }
                else if (current > end)
                {
                    weight = (long)((end - old) * amount);
                }
                yield return resultSelector(item, weight);
            }
        }

        /// <summary>
        /// Load configuration with different Environment Variable
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <returns>IConfigurationRoot</returns>
        public static IConfigurationRoot LoadConfig(string config)
        {
            var env = Environment.GetEnvironmentVariable("NEO_NETWORK");
            var configFile = string.IsNullOrWhiteSpace(env) ? $"{config}.json" : $"{config}.{env}.json";
            return new ConfigurationBuilder()
                .AddJsonFile(configFile, true)
                .Build();
        }
    }
}
