// Copyright (C) 2015-2024 The Neo Project.
//
// Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;

namespace Neo
{
    /// <summary>
    /// A helper class that provides common functions.
    /// </summary>
    public static class Helper
    {
        private static readonly DateTime unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        internal static void Remove<T>(this HashSet<T> set, HashSetCache<T> other)
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

        /// <summary>
        /// Converts a hex <see cref="string"/> to byte array.
        /// </summary>
        /// <param name="value">The hex <see cref="string"/> to convert.</param>
        /// <returns>The converted byte array.</returns>
        public static byte[] HexToBytes(this string value)
        {
            if (value == null || value.Length == 0)
                return Array.Empty<byte>();
            if (value.Length % 2 == 1)
                throw new FormatException();
            byte[] result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return result;
        }

        internal static BigInteger NextBigInteger(this Random rand, int sizeInBits)
        {
            if (sizeInBits < 0)
                throw new ArgumentException("sizeInBits must be non-negative");
            if (sizeInBits == 0)
                return 0;
            Span<byte> b = stackalloc byte[sizeInBits / 8 + 1];
            rand.NextBytes(b);
            if (sizeInBits % 8 == 0)
                b[^1] = 0;
            else
                b[^1] &= (byte)((1 << sizeInBits % 8) - 1);
            return new BigInteger(b);
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> to timestamp.
        /// </summary>
        /// <param name="time">The <see cref="DateTime"/> to convert.</param>
        /// <returns>The converted timestamp.</returns>
        public static uint ToTimestamp(this DateTime time)
        {
            return (uint)(time.ToUniversalTime() - unixEpoch).TotalSeconds;
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> to timestamp in milliseconds.
        /// </summary>
        /// <param name="time">The <see cref="DateTime"/> to convert.</param>
        /// <returns>The converted timestamp.</returns>
        public static ulong ToTimestampMS(this DateTime time)
        {
            return (ulong)(time.ToUniversalTime() - unixEpoch).TotalMilliseconds;
        }

        /// <summary>
        /// Checks if address is IPv4 Mapped to IPv6 format, if so, Map to IPv4.
        /// Otherwise, return current address.
        /// </summary>
        internal static IPAddress Unmap(this IPAddress address)
        {
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            return address;
        }

        /// <summary>
        /// Checks if IPEndPoint is IPv4 Mapped to IPv6 format, if so, unmap to IPv4.
        /// Otherwise, return current endpoint.
        /// </summary>
        internal static IPEndPoint Unmap(this IPEndPoint endPoint)
        {
            if (!endPoint.Address.IsIPv4MappedToIPv6)
                return endPoint;
            return new IPEndPoint(endPoint.Address.Unmap(), endPoint.Port);
        }
    }
}
