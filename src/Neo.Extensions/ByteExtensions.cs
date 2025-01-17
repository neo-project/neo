// Copyright (C) 2015-2025 The Neo Project.
//
// ByteExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.Extensions
{
    public static class ByteExtensions
    {
        private const int DefaultXxHash3Seed = 40343;
        private const string s_hexChars = "0123456789abcdef";

        /// <summary>
        /// Computes the 32-bit hash value for the specified byte array using the xxhash3 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the xxhash3 algorithm.</param>
        /// <returns>The computed hash code.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int XxHash3_32(this ReadOnlySpan<byte> value, long seed = DefaultXxHash3Seed)
        {
            return HashCode.Combine(XxHash3.HashToUInt64(value, seed));
        }

        /// <summary>
        /// Computes the 32-bit hash value for the specified byte array using the xxhash3 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the xxhash3 algorithm.</param>
        /// <returns>The computed hash code.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int XxHash3_32(this byte[] value, long seed = DefaultXxHash3Seed)
        {
            return XxHash3_32(value.AsSpan(), seed);
        }

        /// <summary>
        /// Converts a byte array to hex <see cref="string"/>.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>The converted hex <see cref="string"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToHexString(this byte[] value)
        {
#if NET9_0_OR_GREATER
            return Convert.ToHexStringLower(value);
#else
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return string.Create(value.Length * 2, value, (span, bytes) =>
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    var b = bytes[i];
                    span[i * 2] = s_hexChars[b >> 4];
                    span[i * 2 + 1] = s_hexChars[b & 0xF];
                }
            });
#endif
        }

        /// <summary>
        /// Converts a byte array to hex <see cref="string"/>.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <param name="reverse">Indicates whether it should be converted in the reversed byte order.</param>
        /// <returns>The converted hex <see cref="string"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToHexString(this byte[] value, bool reverse = false)
        {
            if (!reverse)
                return ToHexString(value);

            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return string.Create(value.Length * 2, value, (span, bytes) =>
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    var b = bytes[bytes.Length - i - 1];
                    span[i * 2] = s_hexChars[b >> 4];
                    span[i * 2 + 1] = s_hexChars[b & 0xF];
                }
            });
        }

        /// <summary>
        /// Converts a byte array to hex <see cref="string"/>.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>The converted hex <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToHexString(this ReadOnlySpan<byte> value)
        {
#if NET9_0_OR_GREATER
            return Convert.ToHexStringLower(value);
#else
            // string.Create with ReadOnlySpan<char> not supported in NET5 or lower
            var sb = new StringBuilder(value.Length * 2);
            for (var i = 0; i < value.Length; i++)
            {
                var b = value[i];
                sb.Append(s_hexChars[b >> 4]);
                sb.Append(s_hexChars[b & 0xF]);
            }
            return sb.ToString();
#endif
        }
    }
}
