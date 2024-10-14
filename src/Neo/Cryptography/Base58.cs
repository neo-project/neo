// Copyright (C) 2015-2024 The Neo Project.
//
// Base58.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Neo.Cryptography
{
    /// <summary>
    /// A helper class for base-58 encoder.
    /// </summary>
    public static class Base58
    {
        /// <summary>
        /// Represents the alphabet of the base-58 encoder.
        /// </summary>
        public const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        private const char ZeroChar = '1';
        private static readonly IReadOnlyDictionary<char, int> s_alphabetDic = Enumerable.Range(0, Alphabet.Length).ToDictionary(t => Alphabet[t], t => t);

        /// <summary>
        /// Converts the specified <see cref="string"/>, which encodes binary data as base-58 digits, to an equivalent byte array. The encoded <see cref="string"/> contains the checksum of the binary data.
        /// </summary>
        /// <param name="input">The <see cref="string"/> to convert.</param>
        /// <returns>A byte array that is equivalent to <paramref name="input"/>.</returns>
        public static byte[] Base58CheckDecode(this string input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            byte[] buffer = Decode(input);
            if (buffer.Length < 4) throw new FormatException();
            byte[] checksum = buffer.Sha256(0, buffer.Length - 4).Sha256();
            if (!buffer.AsSpan(^4).SequenceEqual(checksum.AsSpan(..4)))
                throw new FormatException();
            var ret = buffer[..^4];
            Array.Clear(buffer, 0, buffer.Length);
            return ret;
        }

        /// <summary>
        /// Converts a byte array to its equivalent <see cref="string"/>
        /// representation that is encoded with base-58 digits.
        /// The encoded <see cref="string"/> contains the checksum of the binary data.
        /// </summary>
        /// <param name="data">The byte array to convert.</param>
        /// <returns>The <see cref="string"/> representation, in base-58, of the contents of <paramref name="data"/>.</returns>
        public static string Base58CheckEncode(this ReadOnlySpan<byte> data)
        {
            byte[] checksum = data.Sha256().Sha256();
            Span<byte> buffer = stackalloc byte[data.Length + 4];
            data.CopyTo(buffer);
            checksum.AsSpan(..4).CopyTo(buffer[data.Length..]);
            var ret = Encode(buffer);
            buffer.Clear();
            return ret;
        }

        /// <summary>
        /// Converts the specified <see cref="string"/>, which encodes binary data as base-58 digits, to an equivalent byte array.
        /// </summary>
        /// <param name="input">The <see cref="string"/> to convert.</param>
        /// <returns>A byte array that is equivalent to <paramref name="input"/>.</returns>
        public static byte[] Decode(string input)
        {
            // Decode Base58 string to BigInteger
            var bi = BigInteger.Zero;
            for (int i = 0; i < input.Length; i++)
            {
                if (!s_alphabetDic.TryGetValue(input[i], out var digit))
                    throw new FormatException($"Invalid Base58 character '{input[i]}' at position {i}");
                bi = bi * Alphabet.Length + digit;
            }

            // Encode BigInteger to byte[]
            // Leading zero bytes get encoded as leading `1` characters
            int leadingZeroCount = LeadingBase58Zeros(input);
            if (bi.IsZero)
            {
                return new byte[leadingZeroCount];
            }

            int decodedSize = leadingZeroCount + bi.GetByteCount(true);
            byte[] result = new byte[decodedSize];

            _ = bi.TryWriteBytes(result.AsSpan(leadingZeroCount), out _, true, true);
            return result;
        }

        /// <summary>
        /// Converts a byte array to its equivalent <see cref="string"/> representation that is encoded with base-58 digits.
        /// </summary>
        /// <param name="input">The byte array to convert.</param>
        /// <returns>The <see cref="string"/> representation, in base-58, of the contents of <paramref name="input"/>.</returns>
        public static string Encode(ReadOnlySpan<byte> input)
        {
            // Decode byte[] to BigInteger
            BigInteger value = new(input, isUnsigned: true, isBigEndian: true);

            // Encode BigInteger to Base58 string
            var sb = new StringBuilder(input.Length * 138 / 100 + 5);

            while (value > 0)
            {
                value = BigInteger.DivRem(value, Alphabet.Length, out var remainder);
                sb.Insert(0, Alphabet[(int)remainder]);
            }

            // Append `1` for each leading 0 byte
            for (int i = 0; i < input.Length && input[i] == 0; i++)
            {
                sb.Insert(0, ZeroChar);
            }
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LeadingBase58Zeros(string collection)
        {
            var i = 0;
            var len = collection.Length;
            for (; i < len && collection[i] == ZeroChar; i++) { }

            return i;
        }
    }
}

public static class Ext
{
    public static byte[] Sha256(this ReadOnlySpan<byte> value)
    {
        byte[] buffer = new byte[32];
        using var sha256 = SHA256.Create();
        sha256.TryComputeHash(value, buffer, out _);
        return buffer;
    }



    /// <summary>
    /// Computes the hash value for the specified byte array using the sha256 algorithm.
    /// </summary>
    /// <param name="value">The input to compute the hash code for.</param>
    /// <returns>The computed hash code.</returns>
    public static byte[] Sha256(this byte[] value)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(value);
    }

    /// <summary>
    /// Computes the hash value for the specified region of the specified byte array using the sha256 algorithm.
    /// </summary>
    /// <param name="value">The input to compute the hash code for.</param>
    /// <param name="offset">The offset into the byte array from which to begin using data.</param>
    /// <param name="count">The number of bytes in the array to use as data.</param>
    /// <returns>The computed hash code.</returns>
    public static byte[] Sha256(this byte[] value, int offset, int count)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(value, offset, count);
    }
}
