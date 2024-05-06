// Copyright (C) 2015-2024 The Neo Project.
//
// SpanExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Neo.Hosting.App.Extensions
{
    internal static class SpanExtensions
    {
        private static readonly UTF8Encoding s_utf8NoBom = new(false, true);

        public static int Write<T>(this Span<byte> span, T value, int start = 0)
            where T : unmanaged
        {
            var tSpan = MemoryMarshal.CreateSpan(ref value, 1);
            var bytes = MemoryMarshal.AsBytes(tSpan);
            bytes.CopyTo(span[start..]);
            return bytes.Length;
        }

        public static int Write<T>(this Span<byte> span, ref T value, int start = 0)
            where T : unmanaged
        {
            var tSpan = MemoryMarshal.CreateSpan(ref value, 1);
            var bytes = MemoryMarshal.AsBytes(tSpan);
            bytes.CopyTo(span[start..]);
            return bytes.Length;
        }

        public static int Write<T>(this Span<byte> span, T[] values, int start = 0)
            where T : unmanaged
        {
            var tSpan = values.AsSpan();
            var bytes = MemoryMarshal.AsBytes(tSpan);
            var len = span.Write(values.Length, start);
            bytes.CopyTo(span[(len + start)..]);
            return bytes.Length + len;
        }

        public static int Write<T>(this Span<byte> span, Span<T> values, int start = 0)
            where T : unmanaged
        {
            var bytes = MemoryMarshal.AsBytes(values);
            var len = span.Write(values.Length, start);
            bytes.CopyTo(span[(len + start)..]);
            return bytes.Length + len;
        }

        public static int Write(this Span<byte> span, string value, int start = 0)
        {
            var encoding = s_utf8NoBom;
            var valueSpan = value.AsSpan();
            var encodedLength = encoding.GetByteCount(valueSpan);

            Span<byte> byteSpan = stackalloc byte[encodedLength];
            var byteLength = encoding.GetBytes(valueSpan, byteSpan);

            var len = span.Write(byteLength, start);
            byteSpan.CopyTo(span[(len + start)..]);
            return byteLength + len;
        }

        public static int Write(this Span<byte> span, char[] value, int start = 0)
        {
            var encoding = s_utf8NoBom;
            var valueSpan = value.AsSpan();
            var encodedLength = encoding.GetByteCount(valueSpan);

            Span<byte> byteSpan = stackalloc byte[encodedLength];
            var byteLength = encoding.GetBytes(valueSpan, byteSpan);

            var len = span.Write(byteLength, start);
            len += span.Write(value.Length, start + len);
            byteSpan.CopyTo(span[(len + start)..]);
            return byteLength + len;
        }

        public static int Read<T>(this Span<byte> span, ref T result, int start = 0)
            where T : unmanaged
        {
            var tSpan = MemoryMarshal.CreateSpan(ref result, 1);
            var bytes = MemoryMarshal.AsBytes(tSpan);
            span.Slice(start, bytes.Length).CopyTo(bytes);
            return bytes.Length;
        }

        public static T Read<T>(this Span<byte> span, int start = 0)
            where T : unmanaged
        {
            var result = default(T);
            var tSpan = MemoryMarshal.CreateSpan(ref result, 1);
            var bytes = MemoryMarshal.AsBytes(tSpan);
            span.Slice(start, bytes.Length).CopyTo(bytes);
            return result;
        }

        public static int ReadCharArray(this Span<byte> span, out char[] results, int start = 0)
        {
            var byteLength = span.Read<int>(start);
            var charLength = span.Read<int>(start + sizeof(int));

            Span<byte> byteSpan = stackalloc byte[byteLength];
            span.Slice(sizeof(int) * 2 + start, byteLength).CopyTo(byteSpan);

            results = new char[charLength];
            var charSpan = results.AsSpan();

            return s_utf8NoBom.GetChars(byteSpan, charSpan) + (sizeof(int) * 2);
        }

        public static char[] ReadCharArray(this Span<byte> span, int start = 0)
        {
            span.ReadCharArray(out var results, start);
            return results;
        }

        public static int ReadString(this Span<byte> span, out string result, int start = 0)
        {
            var byteLength = span.Read<int>(start);
            Span<byte> bytes = stackalloc byte[byteLength];
            span.Slice(sizeof(int) + start, byteLength).CopyTo(bytes);
            result = s_utf8NoBom.GetString(bytes);
            return byteLength + sizeof(int);
        }

        public static string ReadString(this Span<byte> span, int start = 0)
        {
            span.ReadString(out var result, start);
            return result;
        }

        public static int ReadArray<T>(this Span<byte> span, out T[] results, int start = 0)
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
                throw new InvalidCastException();

            var length = span.Read<int>(start);
            results = new T[length];

            var tSpan = results.AsSpan();
            var bytes = MemoryMarshal.AsBytes(tSpan);
            span.Slice(sizeof(int) + start, bytes.Length).CopyTo(bytes);

            return bytes.Length + sizeof(int);
        }

        public static T[] ReadArray<T>(this Span<byte> span, int start = 0)
            where T : unmanaged
        {
            span.ReadArray(out T[] results, start);
            return results;
        }
    }
}
