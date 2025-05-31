// Copyright (C) 2015-2025 The Neo Project.
//
// StdLib.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using Microsoft.IdentityModel.Tokens;
using Neo.Cryptography;
using Neo.Json;
using Neo.VM.Types;
using System;
using System.Globalization;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract library that provides useful functions.
    /// </summary>
    public sealed class StdLib : NativeContract
    {
        private const int MaxInputLength = 1024;

        internal StdLib() : base() { }

        [ContractMethod(CpuFee = 1 << 12)]
        private static byte[] Serialize(ApplicationEngine engine, StackItem item)
        {
            return BinarySerializer.Serialize(item, engine.Limits);
        }

        [ContractMethod(CpuFee = 1 << 14)]
        private static StackItem Deserialize(ApplicationEngine engine, byte[] data)
        {
            return BinarySerializer.Deserialize(data, engine.Limits, engine.ReferenceCounter);
        }

        [ContractMethod(CpuFee = 1 << 12)]
        private static byte[] JsonSerialize(ApplicationEngine engine, StackItem item)
        {
            return JsonSerializer.SerializeToByteArray(item, engine.Limits.MaxItemSize);
        }

        [ContractMethod(CpuFee = 1 << 14)]
        private static StackItem JsonDeserialize(ApplicationEngine engine, byte[] json)
        {
            return JsonSerializer.Deserialize(engine, JToken.Parse(json, 10), engine.Limits, engine.ReferenceCounter);
        }

        /// <summary>
        /// Converts an integer to a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The integer to convert.</param>
        /// <returns>The converted <see cref="string"/>.</returns>
        [ContractMethod(CpuFee = 1 << 12)]
        public static string Itoa(BigInteger value)
        {
            return Itoa(value, 10);
        }

        /// <summary>
        /// Converts an integer to a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The integer to convert.</param>
        /// <param name="base">The base of the integer. Only support 10 and 16.</param>
        /// <returns>The converted <see cref="string"/>.</returns>
        [ContractMethod(CpuFee = 1 << 12)]
        public static string Itoa(BigInteger value, int @base)
        {
            return @base switch
            {
                10 => value.ToString(),
                16 => value.ToString("x"),
                _ => throw new ArgumentOutOfRangeException(nameof(@base))
            };
        }

        /// <summary>
        /// Converts a <see cref="string"/> to an integer.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to convert.</param>
        /// <returns>The converted integer.</returns>
        [ContractMethod(CpuFee = 1 << 6)]
        public static BigInteger Atoi([MaxLength(MaxInputLength)] string value)
        {
            return Atoi(value, 10);
        }

        /// <summary>
        /// Converts a <see cref="string"/> to an integer.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to convert.</param>
        /// <param name="base">The base of the integer. Only support 10 and 16.</param>
        /// <returns>The converted integer.</returns>
        [ContractMethod(CpuFee = 1 << 6)]
        public static BigInteger Atoi([MaxLength(MaxInputLength)] string value, int @base)
        {
            return @base switch
            {
                10 => BigInteger.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                16 => BigInteger.Parse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture),
                _ => throw new ArgumentOutOfRangeException(nameof(@base))
            };
        }

        /// <summary>
        /// Encodes a byte array into a base64 <see cref="string"/>.
        /// </summary>
        /// <param name="data">The byte array to be encoded.</param>
        /// <returns>The encoded <see cref="string"/>.</returns>
        [ContractMethod(CpuFee = 1 << 5)]
        public static string Base64Encode([MaxLength(MaxInputLength)] byte[] data)
        {
            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// Decodes a byte array from a base64 <see cref="string"/>.
        /// </summary>
        /// <param name="s">The base64 <see cref="string"/>.</param>
        /// <returns>The decoded byte array.</returns>
        [ContractMethod(CpuFee = 1 << 5)]
        public static byte[] Base64Decode([MaxLength(MaxInputLength)] string s)
        {
            return Convert.FromBase64String(s);
        }

        /// <summary>
        /// Encodes a byte array into a base64Url string.
        /// </summary>
        /// <param name="data">The base64Url to be encoded.</param>
        /// <returns>The encoded base64Url string.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 5)]
        public static string Base64UrlEncode([MaxLength(MaxInputLength)] string data)
        {
            return Base64UrlEncoder.Encode(data);
        }

        /// <summary>
        /// Decodes a byte array from a base64Url string.
        /// </summary>
        /// <param name="s">The base64Url string.</param>
        /// <returns>The decoded base64Url string.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 5)]
        public static string Base64UrlDecode([MaxLength(MaxInputLength)] string s)
        {
            return Base64UrlEncoder.Decode(s);
        }

        /// <summary>
        /// Encodes a byte array into a base58 <see cref="string"/>.
        /// </summary>
        /// <param name="data">The byte array to be encoded.</param>
        /// <returns>The encoded <see cref="string"/>.</returns>
        [ContractMethod(CpuFee = 1 << 13)]
        public static string Base58Encode([MaxLength(MaxInputLength)] byte[] data)
        {
            return Base58.Encode(data);
        }

        /// <summary>
        /// Decodes a byte array from a base58 <see cref="string"/>.
        /// </summary>
        /// <param name="s">The base58 <see cref="string"/>.</param>
        /// <returns>The decoded byte array.</returns>
        [ContractMethod(CpuFee = 1 << 10)]
        public static byte[] Base58Decode([MaxLength(MaxInputLength)] string s)
        {
            return Base58.Decode(s);
        }

        /// <summary>
        /// Converts a byte array to its equivalent <see cref="string"/> representation that is encoded with base-58 digits. The encoded <see cref="string"/> contains the checksum of the binary data.
        /// </summary>
        /// <param name="data">The byte array to be encoded.</param>
        /// <returns>The encoded <see cref="string"/>.</returns>
        [ContractMethod(CpuFee = 1 << 16)]
        public static string Base58CheckEncode([MaxLength(MaxInputLength)] byte[] data)
        {
            return Base58.Base58CheckEncode(data);
        }

        /// <summary>
        /// Converts the specified <see cref="string"/>, which encodes binary data as base-58 digits, to an equivalent byte array. The encoded <see cref="string"/> contains the checksum of the binary data.
        /// </summary>
        /// <param name="s">The base58 <see cref="string"/>.</param>
        /// <returns>The decoded byte array.</returns>
        [ContractMethod(CpuFee = 1 << 16)]
        public static byte[] Base58CheckDecode([MaxLength(MaxInputLength)] string s)
        {
            return Base58.Base58CheckDecode(s);
        }

        [ContractMethod(CpuFee = 1 << 5)]
        private static int MemoryCompare([MaxLength(MaxInputLength)] byte[] str1, [MaxLength(MaxInputLength)] byte[] str2)
        {
            return Math.Sign(str1.AsSpan().SequenceCompareTo(str2));
        }

        [ContractMethod(CpuFee = 1 << 6)]
        private static int MemorySearch([MaxLength(MaxInputLength)] byte[] mem, byte[] value)
        {
            return MemorySearch(mem, value, 0, false);
        }

        [ContractMethod(CpuFee = 1 << 6)]
        private static int MemorySearch([MaxLength(MaxInputLength)] byte[] mem, byte[] value, int start)
        {
            return MemorySearch(mem, value, start, false);
        }

        [ContractMethod(CpuFee = 1 << 6)]
        private static int MemorySearch([MaxLength(MaxInputLength)] byte[] mem, byte[] value, int start, bool backward)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (backward)
            {
                return mem.AsSpan(0, start).LastIndexOf(value);
            }
            else
            {
                int index = mem.AsSpan(start).IndexOf(value);
                if (index < 0) return -1;
                return index + start;
            }
        }

        [ContractMethod(CpuFee = 1 << 8)]
        private static string[] StringSplit([MaxLength(MaxInputLength)] string str, string separator)
        {
            if (separator is null) throw new ArgumentNullException(nameof(separator));
            return str.Split(separator);
        }

        [ContractMethod(CpuFee = 1 << 8)]
        private static string[] StringSplit([MaxLength(MaxInputLength)] string str, string separator, bool removeEmptyEntries)
        {
            if (separator is null) throw new ArgumentNullException(nameof(separator));
            StringSplitOptions options = removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;
            return str.Split(separator, options);
        }

        [ContractMethod(CpuFee = 1 << 8)]
        private static int StrLen([MaxLength(MaxInputLength)] string str)
        {
            // return the length of the string in elements
            // it should return 1 for both  "🦆" and "ã"

            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(str);
            int count = 0;

            while (enumerator.MoveNext())
            {
                count++;
            }

            return count;
        }

        [ContractMethod(CpuFee = 1 << 10)]
        public static ulong GetRandom(ApplicationEngine engine, ulong minValue, ulong maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            return GetRandom(engine, maxValue - minValue) + minValue;
        }

        [ContractMethod(CpuFee = 1 << 10)]
        private static ulong GetRandom(ApplicationEngine engine, ulong maxValue)
        {
            var randomProduct = BigMul(maxValue, (ulong)(engine.GetRandom() & ulong.MaxValue), out var lowPart);

            if (lowPart < maxValue)
            {
                var remainder = (0ul - maxValue) % maxValue;

                while (lowPart < remainder)
                {
                    randomProduct = BigMul(maxValue, (ulong)(engine.GetRandom() & ulong.MaxValue), out lowPart);
                }
            }

            return randomProduct;

            static ulong BigMul(ulong a, ulong b, out ulong low)
            {
                // Adaptation of algorithm for multiplication
                // of 32-bit unsigned integers described
                // in Hacker's Delight by Henry S. Warren, Jr. (ISBN 0-201-91465-4), Chapter 8
                // Basically, it's an optimized version of FOIL method applied to
                // low and high dwords of each operand

                // Use 32-bit uints to optimize the fallback for 32-bit platforms.
                var al = (uint)a;
                var ah = (uint)(a >> 32);
                var bl = (uint)b;
                var bh = (uint)(b >> 32);

                var mull = ((ulong)al) * bl;
                var t = ((ulong)ah) * bl + (mull >> 32);
                var tl = ((ulong)al) * bh + (uint)t;

                low = tl << 32 | (uint)mull;

                return ((ulong)ah) * bh + (t >> 32) + (tl >> 32);
            }
        }
    }
}
