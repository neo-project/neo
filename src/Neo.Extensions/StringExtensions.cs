// Copyright (C) 2015-2025 The Neo Project.
//
// StringExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.CompilerServices;
#if !NET9_0_OR_GREATER
using System.Globalization;
#endif

namespace Neo.Extensions
{
    public static class StringExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] HexToBytes(this string? value) => HexToBytes(value.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] HexToBytesReversed(this ReadOnlySpan<char> value)
        {
            var data = HexToBytes(value);
            Array.Reverse(data);
            return data;
        }

        /// <summary>
        /// Converts a hex <see cref="string"/> to byte array.
        /// </summary>
        /// <param name="value">The hex <see cref="string"/> to convert.</param>
        /// <returns>The converted byte array.</returns>
        public static byte[] HexToBytes(this ReadOnlySpan<char> value)
        {
#if !NET9_0_OR_GREATER
            if (value.IsEmpty)
                return [];
            if (value.Length % 2 == 1)
                throw new FormatException();
            var result = new byte[value.Length / 2];
            for (var i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Slice(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return result;
#else
            return Convert.FromHexString(value);
#endif
        }

        /// <summary>
        /// Gets the size of the specified <see cref="string"/> encoded in variable-length encoding.
        /// </summary>
        /// <param name="value">The specified <see cref="string"/>.</param>
        /// <returns>The size of the <see cref="string"/>.</returns>
        public static int GetVarSize(this string value)
        {
            var size = Utility.StrictUTF8.GetByteCount(value);
            return size.GetVarSize() + size;
        }
    }
}
