#pragma warning disable IDE0051

using Neo.Cryptography;
using Neo.IO.Json;
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

        internal StdLib() { }

        [ContractMethod(CpuFee = 1 << 12)]
        private static byte[] Serialize(ApplicationEngine engine, StackItem item)
        {
            return BinarySerializer.Serialize(item, engine.Limits.MaxItemSize);
        }

        [ContractMethod(CpuFee = 1 << 14)]
        private static StackItem Deserialize(ApplicationEngine engine, byte[] data)
        {
            return BinarySerializer.Deserialize(data, engine.Limits.MaxStackSize, engine.ReferenceCounter);
        }

        [ContractMethod(CpuFee = 1 << 12)]
        private static byte[] JsonSerialize(ApplicationEngine engine, StackItem item)
        {
            return JsonSerializer.SerializeToByteArray(item, engine.Limits.MaxItemSize);
        }

        [ContractMethod(CpuFee = 1 << 14)]
        private static StackItem JsonDeserialize(ApplicationEngine engine, byte[] json)
        {
            return JsonSerializer.Deserialize(JObject.Parse(json, 10), engine.ReferenceCounter);
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
        [ContractMethod(CpuFee = 1 << 12)]
        public static BigInteger Atoi(string value)
        {
            return Atoi(value, 10);
        }

        /// <summary>
        /// Converts a <see cref="string"/> to an integer.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to convert.</param>
        /// <param name="base">The base of the integer. Only support 10 and 16.</param>
        /// <returns>The converted integer.</returns>
        [ContractMethod(CpuFee = 1 << 12)]
        public static BigInteger Atoi(string value, int @base)
        {
            CheckInput(value);
            return @base switch
            {
                10 => BigInteger.Parse(value),
                16 => BigInteger.Parse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture),
                _ => throw new ArgumentOutOfRangeException(nameof(@base))
            };
        }

        /// <summary>
        /// Encodes a byte array into a base64 <see cref="string"/>.
        /// </summary>
        /// <param name="data">The byte array to be encoded.</param>
        /// <returns>The encoded <see cref="string"/>.</returns>
        [ContractMethod(CpuFee = 1 << 12)]
        public static string Base64Encode(byte[] data)
        {
            CheckInput(data);
            return Convert.ToBase64String(data);
        }

        /// <summary>
        /// Decodes a byte array from a base64 <see cref="string"/>.
        /// </summary>
        /// <param name="s">The base64 <see cref="string"/>.</param>
        /// <returns>The decoded byte array.</returns>
        [ContractMethod(CpuFee = 1 << 12)]
        public static byte[] Base64Decode(string s)
        {
            CheckInput(s);
            return Convert.FromBase64String(s);
        }

        /// <summary>
        /// Encodes a byte array into a base58 <see cref="string"/>.
        /// </summary>
        /// <param name="data">The byte array to be encoded.</param>
        /// <returns>The encoded <see cref="string"/>.</returns>
        [ContractMethod(CpuFee = 1 << 12)]
        public static string Base58Encode(byte[] data)
        {
            CheckInput(data);
            return Base58.Encode(data);
        }

        /// <summary>
        /// Decodes a byte array from a base58 <see cref="string"/>.
        /// </summary>
        /// <param name="s">The base58 <see cref="string"/>.</param>
        /// <returns>The decoded byte array.</returns>
        [ContractMethod(CpuFee = 1 << 12)]
        public static byte[] Base58Decode(string s)
        {
            CheckInput(s);
            return Base58.Decode(s);
        }

        [ContractMethod(CpuFee = 1 << 12)]
        private static int MemoryCompare(byte[] str1, byte[] str2)
        {
            CheckInput(str1);
            CheckInput(str2);
            return Math.Sign(str1.AsSpan().SequenceCompareTo(str2));
        }

        [ContractMethod(CpuFee = 1 << 12)]
        private static int MemorySearch(byte[] mem, byte[] value)
        {
            return MemorySearch(mem, value, 0, false);
        }

        [ContractMethod(CpuFee = 1 << 12)]
        private static int MemorySearch(byte[] mem, byte[] value, int start)
        {
            return MemorySearch(mem, value, start, false);
        }

        [ContractMethod(CpuFee = 1 << 12)]
        private static int MemorySearch(byte[] mem, byte[] value, int start, bool backward)
        {
            CheckInput(mem);
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

        [ContractMethod(CpuFee = 1 << 12)]
        private static string[] StringSplit(string str, string separator)
        {
            CheckInput(str);
            return str.Split(separator);
        }

        private static void CheckInput(string input)
        {
            if (Utility.StrictUTF8.GetByteCount(input) > MaxInputLength)
                throw new InvalidOperationException("The input exceeds the maximum length.");
        }

        private static void CheckInput(byte[] input)
        {
            if (input.Length > MaxInputLength)
                throw new InvalidOperationException("The input exceeds the maximum length.");
        }
    }
}
