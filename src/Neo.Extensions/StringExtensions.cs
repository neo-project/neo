// Copyright (C) 2015-2026 The Neo Project.
//
// StringExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo;

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
    public static string ToStrictUtf8String(this ReadOnlySpan<byte> value)
    {
        try
        {
            return StrictUTF8.GetString(value);
        }
        catch (DecoderFallbackException ex)
        {
            var bytesInfo = value.Length <= 32 ? $"Bytes: [{string.Join(", ", value.ToArray().Select(b => $"0x{b:X2}"))}]" : $"Length: {value.Length} bytes";
            throw new DecoderFallbackException($"Failed to decode byte span to UTF-8 string (strict mode): The input contains invalid UTF-8 byte sequences. {bytesInfo}. Ensure all bytes form valid UTF-8 character sequences.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Invalid byte span provided for UTF-8 decoding. The span may be corrupted or contain invalid data.", nameof(value), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while decoding byte span to UTF-8 string in strict mode. This may indicate a system-level encoding issue.", ex);
        }
    }

    /// <summary>
    /// Converts a byte array to a strict UTF8 string.
    /// </summary>
    /// <param name="value">The byte array to convert.</param>
    /// <returns>The converted string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToStrictUtf8String(this byte[] value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Cannot decode null byte array to UTF-8 string.");

        try
        {
            return StrictUTF8.GetString(value);
        }
        catch (DecoderFallbackException ex)
        {
            var bytesInfo = value.Length <= 32 ? $"Bytes: {BitConverter.ToString(value)}" : $"Length: {value.Length} bytes, First 16: {BitConverter.ToString(value, 0, Math.Min(16, value.Length))}...";
            throw new DecoderFallbackException($"Failed to decode byte array to UTF-8 string (strict mode): The input contains invalid UTF-8 byte sequences. {bytesInfo}. Ensure all bytes form valid UTF-8 character sequences.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Invalid byte array provided for UTF-8 decoding. The array may be corrupted or contain invalid data.", nameof(value), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while decoding byte array to UTF-8 string in strict mode. This may indicate a system-level encoding issue.", ex);
        }
    }

    /// <summary>
    /// Converts a byte array to a strict UTF8 string.
    /// </summary>
    /// <param name="value">The byte array to convert.</param>
    /// <param name="start">The start index of the byte array.</param>
    /// <param name="count">The count of the byte array.</param>
    /// <returns>The converted string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToStrictUtf8String(this byte[] value, int start, int count)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Cannot decode null byte array to UTF-8 string.");
        if (start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start index cannot be negative.");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");
        if (start + count > value.Length)
            throw new ArgumentOutOfRangeException(nameof(count), $"The specified range [{start}, {start + count}) exceeds the array bounds (length: {value.Length}). Ensure start + count <= array.Length.");

        try
        {
            return StrictUTF8.GetString(value, start, count);
        }
        catch (DecoderFallbackException ex)
        {
            var rangeBytes = new byte[count];
            Array.Copy(value, start, rangeBytes, 0, count);
            var bytesInfo = count <= 32 ? $"Bytes: {BitConverter.ToString(rangeBytes)}" : $"Length: {count} bytes, First 16: {BitConverter.ToString(rangeBytes, 0, Math.Min(16, count))}...";
            throw new DecoderFallbackException($"Failed to decode byte array range [{start}, {start + count}) to UTF-8 string (strict mode): The input contains invalid UTF-8 byte sequences. {bytesInfo}. Ensure all bytes form valid UTF-8 character sequences.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid parameters provided for UTF-8 decoding. Array length: {value.Length}, Start: {start}, Count: {count}.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An unexpected error occurred while decoding byte array range [{start}, {start + count}) to UTF-8 string in strict mode. This may indicate a system-level encoding issue.", ex);
        }
    }

    /// <summary>
    /// Converts a string to a strict UTF8 byte array.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The converted byte array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ToStrictUtf8Bytes(this string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Cannot encode null string to UTF-8 bytes.");

        try
        {
            return StrictUTF8.GetBytes(value);
        }
        catch (EncoderFallbackException ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters, First 50: '{value[..50]}...'";
            throw new EncoderFallbackException($"Failed to encode string to UTF-8 bytes (strict mode): The input contains characters that cannot be encoded in UTF-8. {valueInfo}. Ensure the string contains only valid Unicode characters.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Invalid string provided for UTF-8 encoding. The string may contain unsupported characters.", nameof(value), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while encoding string to UTF-8 bytes in strict mode. This may indicate a system-level encoding issue.", ex);
        }
    }

    /// <summary>
    /// Gets the size of the specified <see cref="string"/> encoded in strict UTF8.
    /// </summary>
    /// <param name="value">The specified <see cref="string"/>.</param>
    /// <returns>The size of the <see cref="string"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStrictUtf8ByteCount(this string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Cannot get UTF-8 byte count for null string.");

        try
        {
            return StrictUTF8.GetByteCount(value);
        }
        catch (EncoderFallbackException ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters, First 50: '{value[..50]}...'";
            throw new EncoderFallbackException($"Failed to get UTF-8 byte count for string (strict mode): The input contains characters that cannot be encoded in UTF-8. {valueInfo}. Ensure the string contains only valid Unicode characters.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("Invalid string provided for UTF-8 byte count calculation. The string may contain unsupported characters.", nameof(value), ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred while calculating UTF-8 byte count for string in strict mode. This may indicate a system-level encoding issue.", ex);
        }
    }

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
    public static byte[] HexToBytes(this string? value)
    {
        if (value == null)
            return [];

        try
        {
            return HexToBytes(value.AsSpan());
        }
        catch (ArgumentException ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters";
            throw new ArgumentException($"Failed to convert hex string to bytes: The input has an invalid length (must be even) or contains non-hexadecimal characters. {valueInfo}. Valid hex characters are 0-9, A-F, and a-f.", nameof(value), ex);
        }
        catch (FormatException ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters";
            throw new FormatException($"Failed to convert hex string to bytes: The input contains invalid hexadecimal characters. {valueInfo}. Valid hex characters are 0-9, A-F, and a-f.", ex);
        }
        catch (Exception ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters";
            throw new InvalidOperationException($"An unexpected error occurred while converting hex string to bytes. {valueInfo}. This may indicate a system-level parsing issue.", ex);
        }
    }

    /// <summary>
    /// Converts a hex <see cref="string"/> to byte array then reverses the order of the bytes.
    /// </summary>
    /// <param name="value">The hex <see cref="string"/> to convert.</param>
    /// <returns>The converted reversed byte array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] HexToBytesReversed(this ReadOnlySpan<char> value)
    {
        try
        {
            var data = HexToBytes(value);
            Array.Reverse(data);
            return data;
        }
        catch (ArgumentException ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters";
            throw new ArgumentException($"Failed to convert hex span to reversed bytes: The input has an invalid length (must be even) or contains non-hexadecimal characters. {valueInfo}. Valid hex characters are 0-9, A-F, and a-f.", ex);
        }
        catch (FormatException ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters";
            throw new FormatException($"Failed to convert hex span to reversed bytes: The input contains invalid hexadecimal characters. {valueInfo}. Valid hex characters are 0-9, A-F, and a-f.", ex);
        }
        catch (Exception ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters";
            throw new InvalidOperationException($"An unexpected error occurred while converting hex span to reversed bytes. {valueInfo}. This may indicate a system-level parsing or array manipulation issue.", ex);
        }
    }

    /// <summary>
    /// Converts a hex <see cref="string"/> to byte array.
    /// </summary>
    /// <param name="value">The hex <see cref="string"/> to convert.</param>
    /// <returns>The converted byte array.</returns>
    public static byte[] HexToBytes(this ReadOnlySpan<char> value)
    {
        try
        {
            return Convert.FromHexString(value);
        }
        catch (ArgumentException ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters";
            throw new ArgumentException($"Failed to convert hex span to bytes: The input has an invalid length (must be even) or contains non-hexadecimal characters. {valueInfo}. Valid hex characters are 0-9, A-F, and a-f.", ex);
        }
        catch (FormatException ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters";
            throw new FormatException($"Failed to convert hex span to bytes: The input contains invalid hexadecimal characters. {valueInfo}. Valid hex characters are 0-9, A-F, and a-f.", ex);
        }
        catch (Exception ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters";
            throw new InvalidOperationException($"An unexpected error occurred while converting hex span to bytes. {valueInfo}. This may indicate a system-level parsing issue.", ex);
        }
    }

    /// <summary>
    /// Gets the size of the specified <see cref="string"/> encoded in variable-length encoding.
    /// </summary>
    /// <param name="value">The specified <see cref="string"/>.</param>
    /// <returns>The size of the <see cref="string"/>.</returns>
    public static int GetVarSize(this string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "Cannot calculate variable size for null string.");

        try
        {
            var size = value.GetStrictUtf8ByteCount();
            return size.GetVarSize() + size;
        }
        catch (EncoderFallbackException ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters, First 50: '{value[..50]}...'";
            throw new EncoderFallbackException($"Failed to calculate variable size: The string contains characters that cannot be encoded in UTF-8 (strict mode). {valueInfo}. Ensure the string contains only valid Unicode characters.", ex);
        }
        catch (Exception ex)
        {
            var valueInfo = value.Length <= 100 ? $"Input: '{value}'" : $"Input length: {value.Length} characters";
            throw new InvalidOperationException($"An unexpected error occurred while calculating variable size for string. {valueInfo}. This may indicate an issue with the string encoding or variable size calculation.", ex);
        }
    }

    /// <summary>
    /// Trims the specified prefix from the start of the <see cref="string"/>, ignoring case.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to trim.</param>
    /// <param name="prefix">The prefix to trim.</param>
    /// <returns>
    /// The trimmed ReadOnlySpan without prefix. If no prefix is found, the input is returned unmodified.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> TrimStartIgnoreCase(this ReadOnlySpan<char> value, ReadOnlySpan<char> prefix)
    {
        if (value.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
            return value[prefix.Length..];
        return value;
    }
}
