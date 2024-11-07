// Copyright (C) 2015-2024 The Neo Project.
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
        public static string ToHexString(this byte[] value)
        {
            StringBuilder sb = new();
            foreach (var b in value)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }

        /// <summary>
        /// Converts a byte array to hex <see cref="string"/>.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <param name="reverse">Indicates whether it should be converted in the reversed byte order.</param>
        /// <returns>The converted hex <see cref="string"/>.</returns>
        public static string ToHexString(this byte[] value, bool reverse = false)
        {
            StringBuilder sb = new();
            for (var i = 0; i < value.Length; i++)
                sb.AppendFormat("{0:x2}", value[reverse ? value.Length - i - 1 : i]);
            return sb.ToString();
        }

        /// <summary>
        /// Converts a byte array to hex <see cref="string"/>.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>The converted hex <see cref="string"/>.</returns>
        public static string ToHexString(this ReadOnlySpan<byte> value)
        {
            StringBuilder sb = new();
            foreach (var b in value)
                sb.AppendFormat("{0:x2}", b);
            return sb.ToString();
        }
    }
}
