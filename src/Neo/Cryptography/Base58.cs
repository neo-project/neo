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
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
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
        private static readonly BigInteger s_alphabetLength = Alphabet.Length;

        #pragma warning disable format
        private static readonly sbyte[] s_decodeMap =
        [
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 1, 2, 3, 4, 5, 6,
            7, 8, -1, -1, -1, -1, -1, -1, -1, 9, 10, 11, 12, 13, 14, 15, 16, -1, 17,
            18, 19, 20, 21, -1, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, -1, -1,
            -1, -1, -1, -1, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, -1, 44, 45,
            46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, -1, -1, -1, -1, -1,
        ];
        #pragma warning restore format

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
                if (input[i] > 127 || s_decodeMap[input[i]] == -1)
                    throw new FormatException($"Invalid Base58 character '{input[i]}' at position {i}");
                bi = bi * s_alphabetLength + s_decodeMap[input[i]];
            }

            // Encode BigInteger to byte[]
            // Leading zero bytes get encoded as leading `1` characters

            int leadingZeroCount = LeadingBase58Zeros(input);
            if (bi.IsZero)
            {
                return new byte[leadingZeroCount];
            }

            byte[] result = new byte[leadingZeroCount + bi.GetByteCount(true)];

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
                value = BigInteger.DivRem(value, s_alphabetLength, out var remainder);
                sb.Append(Alphabet[(int)remainder]);
            }

            // Append `1` for each leading 0 byte
            for (int i = 0; i < input.Length && input[i] == 0; i++)
            {
                sb.Append(ZeroChar);
            }

            Span<char> copy = stackalloc char[sb.Length];
            sb.CopyTo(0, copy, sb.Length);
            copy.Reverse();

            return copy.ToString();
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
