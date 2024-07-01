// Copyright (C) 2015-2024 The Neo Project.
//
// StreamExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Neo.Hosting.App.Extensions
{
    internal static class StreamExtensions
    {
        private static readonly UTF8Encoding s_utf8NoBom = new(false, true);

        public static void Write<T>(this Stream stream, T value)
            where T : unmanaged
        {
            var tSpan = MemoryMarshal.CreateSpan(ref value, 1);
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Write(span);
        }

        public static void Write<T>(this Stream stream, ref T value)
            where T : unmanaged
        {
            var tSpan = MemoryMarshal.CreateSpan(ref value, 1);
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Write(span);
        }

        public static void Write<T>(this Stream stream, T[] values)
            where T : unmanaged
        {
            var tSpan = values.AsSpan();
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Write(values.Length);
            stream.Write(span);
        }

        public static void Write<T>(this Stream stream, Span<T> values)
            where T : unmanaged
        {
            var span = MemoryMarshal.AsBytes(values);
            stream.Write(values.Length);
            stream.Write(span);
        }

        public static void Write(this Stream stream, string value)
        {
            var encoding = s_utf8NoBom;
            var valueSpan = value.AsSpan();
            var encodedLength = encoding.GetByteCount(valueSpan);

            Span<byte> byteSpan = stackalloc byte[encodedLength];
            var byteLength = encoding.GetBytes(valueSpan, byteSpan);

            stream.Write(byteLength);
            stream.Write(byteSpan);
        }

        public static void Write(this Stream stream, char[] value)
        {
            var encoding = s_utf8NoBom;
            var valueSpan = value.AsSpan();
            var encodedLength = encoding.GetByteCount(valueSpan);

            Span<byte> byteSpan = stackalloc byte[encodedLength];
            var byteLength = encoding.GetBytes(valueSpan, byteSpan);

            stream.Write(byteLength);
            stream.Write(value.Length);
            stream.Write(byteSpan);
        }

        public static ref T Read<T>(this Stream stream, ref T result)
            where T : unmanaged
        {
            var tSpan = MemoryMarshal.CreateSpan(ref result, 1);
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Read(span);
            return ref result;
        }

        public static T Read<T>(this Stream stream)
            where T : unmanaged
        {
            var result = default(T);
            var tSpan = MemoryMarshal.CreateSpan(ref result, 1);
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Read(span);
            return result;
        }

        public static char[] ReadCharArray(this Stream stream)
        {
            var byteLength = stream.Read<int>();
            var charLength = stream.Read<int>();

            Span<byte> span = stackalloc byte[byteLength];
            stream.Read(span);

            var results = new char[charLength];
            var charSpan = results.AsSpan();
            s_utf8NoBom.GetChars(span, charSpan);

            return results;
        }

        public static string ReadString(this Stream stream)
        {
            var byteLength = stream.Read<int>();
            Span<byte> bytes = stackalloc byte[byteLength];
            stream.Read(bytes);
            return s_utf8NoBom.GetString(bytes);
        }

        public static T[] ReadArray<T>(this Stream stream)
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
                throw new InvalidCastException();

            var length = stream.Read<int>();
            var results = new T[length];

            var tSpan = results.AsSpan();
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Read(span);

            return results;
        }
    }
}
