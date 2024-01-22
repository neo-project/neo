// Copyright (C) 2015-2024 The Neo Project.
//
// Utility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.Murmur;
using Neo.Cryptography.RIPEMD160;
using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Neo.Cryptography.Utility
{
    public static class Utility
    {

        /// <summary>
        /// Calculates the 160-bit hash value of the specified message.
        /// </summary>
        /// <param name="message">The message to be hashed.</param>
        /// <returns>160-bit hash value.</returns>
        public static byte[] Hash160(ReadOnlySpan<byte> message)
        {
            return message.Sha256().RIPEMD160();
        }

        /// <summary>
        /// Calculates the 256-bit hash value of the specified message.
        /// </summary>
        /// <param name="message">The message to be hashed.</param>
        /// <returns>256-bit hash value.</returns>
        public static byte[] Hash256(ReadOnlySpan<byte> message)
        {
            return message.Sha256().Sha256();
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the ripemd160 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] RIPEMD160(this byte[] value)
        {
            using var ripemd160 = new RIPEMD160Managed();
            return ripemd160.ComputeHash(value);
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the ripemd160 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] RIPEMD160(this ReadOnlySpan<byte> value)
        {
            byte[] source = value.ToArray();
            return source.RIPEMD160();
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the murmur algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the murmur algorithm.</param>
        /// <returns>The computed hash code.</returns>
        public static uint Murmur32(this byte[] value, uint seed)
        {
            using Murmur32 murmur = new(seed);
            return BinaryPrimitives.ReadUInt32LittleEndian(murmur.ComputeHash(value));
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the murmur algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the murmur algorithm.</param>
        /// <returns>The computed hash code.</returns>
        public static uint Murmur32(this ReadOnlySpan<byte> value, uint seed)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            using Murmur32 murmur = new(seed);
            murmur.TryComputeHash(value, buffer, out _);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        /// <summary>
        /// Computes the 128-bit hash value for the specified byte array using the murmur algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the murmur algorithm.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] Murmur128(this byte[] value, uint seed)
        {
            using Murmur128 murmur = new(seed);
            return murmur.ComputeHash(value);
        }

        /// <summary>
        /// Computes the 128-bit hash value for the specified byte array using the murmur algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the murmur algorithm.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] Murmur128(this ReadOnlySpan<byte> value, uint seed)
        {
            byte[] buffer = new byte[16];
            using Murmur128 murmur = new(seed);
            murmur.TryComputeHash(value, buffer, out _);
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

        /// <summary>
        /// Computes the hash value for the specified byte array using the sha256 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
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
        public static byte[] Sha256(this Span<byte> value)
        {
            return Sha256((ReadOnlySpan<byte>)value);
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

        /// <summary>
        /// Concatenates the specified byte arrays.
        /// </summary>
        /// <param name="buffers">The byte arrays to concatenate.</param>
        /// <returns>The concatenated byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Concat(params byte[][] buffers)
        {
            int length = 0;
            for (int i = 0; i < buffers.Length; i++)
                length += buffers[i].Length;
            byte[] dst = new byte[length];
            int p = 0;
            foreach (byte[] src in buffers)
            {
                Buffer.BlockCopy(src, 0, dst, p, src.Length);
                p += src.Length;
            }
            return dst;
        }

        /// <summary>
        /// Concatenates two byte arrays.
        /// </summary>
        /// <param name="a">The first byte array to concatenate.</param>
        /// <param name="b">The second byte array to concatenate.</param>
        /// <returns>The concatenated byte array.</returns>
        public static byte[] Concat(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            byte[] buffer = new byte[a.Length + b.Length];
            a.CopyTo(buffer);
            b.CopyTo(buffer.AsSpan(a.Length));
            return buffer;
        }

#if !NET5_0_OR_GREATER
        public static int GetBitLength(this BigInteger i)
        {
            byte[] b = i.ToByteArray();
            return (b.Length - 1) * 8 + BitLen(i.Sign > 0 ? b[b.Length - 1] : 255 - b[b.Length - 1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
#endif
    }
}
