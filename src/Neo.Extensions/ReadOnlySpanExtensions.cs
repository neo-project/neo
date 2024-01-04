// Copyright (C) 2015-2024 The Neo Project.
//
// ReadOnlySpanExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Extensions
{
    public static class ReadOnlySpanExtensions
    {
        /// <summary>
        /// Converts a byte array to hex <see cref="string"/>.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>The converted hex <see cref="string"/>.</returns>
        public static string ToHexString(this ReadOnlySpan<byte> value)
        {
            var buf = string.Empty;
            foreach (var b in value)
                buf += $"{b:x02}";
            return buf;
        }

        /// <summary>
        /// Concatenates two byte arrays.
        /// </summary>
        /// <param name="a">The first byte array to concatenate.</param>
        /// <param name="b">The second byte array to concatenate.</param>
        /// <returns>The concatenated byte array.</returns>
        public static byte[] Concat(this ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            var buffer = new byte[a.Length + b.Length];
            a.CopyTo(buffer);
            b.CopyTo(buffer.AsSpan(a.Length));
            return buffer;
        }
    }
}
