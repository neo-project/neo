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
using System.Text;
using System.Diagnostics.CodeAnalysis;

#if !NET9_0_OR_GREATER
using System.Globalization;
#endif

namespace Neo.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// A strict UTF8 encoding
        /// </summary>
        internal static Encoding StrictUTF8 { get; }

        static StringExtensions()
        {
            StrictUTF8 = (Encoding)Encoding.UTF8.Clone();
            StrictUTF8.DecoderFallback = DecoderFallback.ExceptionFallback;
            StrictUTF8.EncoderFallback = EncoderFallback.ExceptionFallback;
        }

        /// <summary>
        /// Converts a byte span to a strict UTF8 string.
        /// </summary>
        /// <param name="bytes">The byte span to convert.</param>
        /// <param name="value">The converted string.</param>
        /// <returns>True if the conversion is successful, otherwise false.</returns>
        public static bool TryToStrictUtf8String(this ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out string? value)
        {
            try
            {
                value = StrictUTF8.GetString(bytes);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Converts a byte span to a strict UTF8 string.
        /// </summary>
        /// <param name="value">The byte span to convert.</param>
        /// <returns>The converted string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToStrictUtf8String(this ReadOnlySpan<byte> value) => StrictUTF8.GetString(value);

        /// <summary>
        /// Converts a byte array to a strict UTF8 string.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>The converted string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToStrictUtf8String(this byte[] value) => StrictUTF8.GetString(value);

        /// <summary>
        /// Converts a byte array to a strict UTF8 string.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <param name="start">The start index of the byte array.</param>
        /// <param name="count">The count of the byte array.</param>
        /// <returns>The converted string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToStrictUtf8String(this byte[] value, int start, int count)
            => StrictUTF8.GetString(value, start, count);

        /// <summary>
        /// Converts a string to a strict UTF8 byte array.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns>The converted byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToStrictUtf8Bytes(this string value) => StrictUTF8.GetBytes(value);

        /// <summary>
        /// Gets the size of the specified <see cref="string"/> encoded in strict UTF8.
        /// </summary>
        /// <param name="value">The specified <see cref="string"/>.</param>
        /// <returns>The size of the <see cref="string"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetStrictUtf8ByteCount(this string value) => StrictUTF8.GetByteCount(value);

        /// <summary>
        /// Determines if the specified <see cref="string"/> is a valid hex string.
        /// </summary>
        /// <param name="value">The specified <see cref="string"/>.</param>
        /// <returns>
        /// True if the <see cref="string"/> is a valid hex string(or empty);
        /// otherwise false(not valid hex string or null).
        /// </returns>
        public static bool IsHex(this string value)
        {
            if (value is null || value.Length % 2 == 1)
                return false;
            foreach (var c in value)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Converts a hex <see cref="string"/> to byte array.
        /// </summary>
        /// <param name="value">The hex <see cref="string"/> to convert.</param>
        /// <returns>The converted byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] HexToBytes(this string? value) => HexToBytes(value.AsSpan());

        /// <summary>
        /// Converts a hex <see cref="string"/> to byte array then reverses the order of the bytes.
        /// </summary>
        /// <param name="value">The hex <see cref="string"/> to convert.</param>
        /// <returns>The converted reversed byte array.</returns>
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
                throw new FormatException($"value.Length({value.Length}) not multiple of 2");
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
            var size = value.GetStrictUtf8ByteCount();
            return size.GetVarSize() + size;
        }

        /// <summary>
        /// Trims the specified prefix from the start of the <see cref="string"/>, ignoring case.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to trim.</param>
        /// <param name="prefix">The prefix to trim.</param>
        /// <returns>The trimmed <see cref="string"/>.</returns>
        public static ReadOnlySpan<char> TrimStartIgnoreCase(this ReadOnlySpan<char> value, ReadOnlySpan<char> prefix)
        {
            if (value.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                return value[prefix.Length..];
            return value;
        }
    }
}
