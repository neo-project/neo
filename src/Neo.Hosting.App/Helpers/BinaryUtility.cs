// Copyright (C) 2015-2024 The Neo Project.
//
// BinaryUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Text;

namespace Neo.Hosting.App.Helpers
{
    internal static class BinaryUtility
    {
        public static int GetByteCount(string? value)
        {
            var count = string.IsNullOrEmpty(value)
                ? 0
                : Encoding.UTF8.GetByteCount(value);

            count += count switch
            {
                <= byte.MaxValue => sizeof(byte) + 1,
                <= ushort.MaxValue and >= byte.MaxValue => sizeof(ushort) + 1,
                _ => sizeof(int) + 1,
            };

            return count;
        }

        public static unsafe T GetByteCount<T>(T value)
            where T : unmanaged
        {
            var srcPointer = (ulong*)&value;
            var dst = new T();
            var dstPointer = (ulong*)&dst;

            *dstPointer = *srcPointer switch
            {
                <= byte.MaxValue => *srcPointer + sizeof(byte) + 1ul,
                <= ushort.MaxValue and >= byte.MaxValue => *srcPointer + sizeof(ushort) + 1ul,
                _ => *srcPointer + sizeof(ulong) + 1ul,
            };

            return *(T*)dstPointer;
        }

        public static unsafe int GetByteCount(int value) =>
            value switch
            {
                <= byte.MaxValue => sizeof(byte) + 1,
                <= ushort.MaxValue and >= byte.MaxValue => sizeof(ushort) + 1,
                _ => sizeof(int) + 1,
            };

        public static unsafe int WriteUtf8String(string? src, int srcOffset, ReadOnlySpan<byte> dst, int dstOffset, int count)
        {
            var size = count switch
            {
                <= byte.MaxValue => sizeof(byte) + 1,
                <= ushort.MaxValue and >= byte.MaxValue => sizeof(ushort) + 1,
                _ => sizeof(int) + 1,
            };

            var valueCount = string.IsNullOrEmpty(src)
                ? 0
                : Encoding.UTF8.GetByteCount(src);

            ArgumentOutOfRangeException.ThrowIfGreaterThan(srcOffset, valueCount, nameof(srcOffset));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(valueCount, srcOffset + count, nameof(count));
            ArgumentOutOfRangeException.ThrowIfZero(dst.Length, nameof(dst));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(dst.Length, size + dstOffset + count, nameof(dst));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(dstOffset, dst.Length, nameof(dstOffset));

            fixed (byte* targetPtr = dst)
            {
                var target = targetPtr + dstOffset;

                switch (count)
                {
                    case <= byte.MaxValue:
                        *target++ = 0xaa;
                        *target++ = (byte)count;
                        break;
                    case <= ushort.MaxValue and >= byte.MaxValue:
                        *target++ = 0xab;
                        *(ushort*)target++ = (ushort)count;
                        break;
                    default:
                        *target++ = 0xac;
                        *(int*)target++ = count;
                        break;
                }

                var srcSpan = new Span<char>(src?.ToCharArray(), srcOffset, count);
                var dstSpan = new Span<byte>(target, count);

                return Encoding.UTF8.GetBytes(srcSpan, dstSpan) + size;
            }
        }
        public static unsafe void WriteEncodedInteger<T>(T value, byte[] dst, int start = 0)
            where T : unmanaged
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(start, dst.Length, nameof(start));

            fixed (byte* targetPtr = dst)
            {
                var target = targetPtr + start;
                var source = (byte*)&value;
                var count = Convert.ToUInt64(value) switch
                {
                    <= byte.MaxValue => sizeof(byte),
                    <= ushort.MaxValue and >= byte.MaxValue => sizeof(ushort),
                    <= uint.MaxValue and >= ushort.MaxValue => sizeof(uint),
                    _ => sizeof(ulong),
                };

                *target++ = count switch
                {
                    sizeof(byte) => 0xfc,
                    sizeof(ushort) => 0xfd,
                    sizeof(uint) => 0xfe,
                    _ => 0xff
                };

                for (; count > 0; count--)
                    *target++ = *source++;
            }
        }

        public static unsafe string? ReadUtf8String(ReadOnlySpan<byte> src, out int count, int start = 0)
        {
            if (src.Length < 2)
                throw new ArgumentOutOfRangeException(nameof(src));

            fixed (byte* sourcePtr = src)
            {
                var source = sourcePtr + start;
                var length = 0;

                switch (*source)
                {
                    case 0xaa:
                        length += *++source;
                        count = sizeof(byte) + 1;
                        break;
                    case 0xab:
                        length += *(ushort*)++source;
                        count = sizeof(ushort) + 1;
                        break;
                    case 0xac:
                        length += *(int*)++source;
                        count = sizeof(int) + 1;
                        break;
                    default:
                        throw new ArgumentException($"Unexpected value 0x{*source:x} at index {start}.", nameof(src));
                }

                if (length == 0)
                    return null;

                if (length < 0)
                    throw new FormatException($"Length {length} is negative.");

                if (length > src.Length)
                    throw new ArgumentOutOfRangeException(nameof(src), length, $"Length {length} exceeds {src.Length}.");

                var result = Encoding.UTF8.GetString(source + count - 1, length);
                count += length;

                return result;
            }
        }

        public static unsafe T ReadEncodedInteger<T>(ReadOnlySpan<byte> src, T max, int start = 0)
            where T : unmanaged
        {
            if (src.Length < 2)
                throw new ArgumentOutOfRangeException(nameof(src));

            fixed (byte* sourcePtr = src)
            {
                var source = sourcePtr + start;
                var readPointer = *source switch
                {
                    0xfc or 0xfd or 0xfe or 0xff => source + 1,
                    _ => throw new ArgumentException($"Unexpected value 0x{*source:x} at index {start}.", nameof(src)),
                };

                var result = *source switch
                {
                    0xfc => *readPointer,
                    0xfd => *(ushort*)readPointer,
                    0xfe => *(uint*)readPointer,
                    _ => *(ulong*)readPointer,
                };

                if (result > *(ulong*)&max)
                    throw new FormatException($"Value {result} is greater than {max}.");

                return (T)Convert.ChangeType(result, typeof(T));
            }
        }
    }
}
