// Copyright (C) 2015-2024 The Neo Project.
//
// MemoryBuffer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.CLI.Pipes.Buffers
{
    internal class MemoryBuffer : IDisposable
    {
        private static readonly UTF8Encoding s_utf8NoBom = new(false, true);

        private readonly MemoryStream _ms;

        public void Dispose()
        {
            _ms.Dispose();
        }

        public MemoryBuffer()
        {
            _ms = new();
        }

        public MemoryBuffer(byte[] buffer)
        {
            _ms = new(buffer);
        }

        public void Write(byte[] buffer) =>
            _ms.Write(buffer, 0, buffer.Length);

        public void Write<T>(T value)
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
                throw new InvalidOperationException("Data type 'char' is not supported.");

            var size = Unsafe.SizeOf<T>();
            var buffer = new byte[size];

            Unsafe.As<byte, T>(ref buffer[0]) = value;

            _ms.Write(buffer, 0, size);
        }

        public void WriteArray<T>(T[] array)
            where T : unmanaged
        {
            Write(array.Length);
            foreach (var item in array)
                Write(item);
        }

        public void WriteString(string value)
        {
            var strByteCount = s_utf8NoBom.GetByteCount(value);
            Write(strByteCount);

            var buffer = s_utf8NoBom.GetBytes(value);
            _ms.Write(buffer, 0, buffer.Length);
        }

        public T Read<T>()
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
                throw new InvalidOperationException("Data type 'char' is not supported.");

            var size = Unsafe.SizeOf<T>();
            var buffer = new byte[size];

            _ms.Read(buffer, 0, size);

            return Unsafe.As<byte, T>(ref buffer[0]);
        }

        public T[] ReadArray<T>()
            where T : unmanaged
        {
            var length = Read<int>();
            var array = new T[length];
            for (var i = 0; i < length; i++)
                array[i] = Read<T>();
            return array;
        }

        public string ReadString()
        {
            var strByteCount = Read<int>();
            var buffer = new byte[strByteCount];
            _ms.Read(buffer, 0, strByteCount);
            return s_utf8NoBom.GetString(buffer);
        }

        public byte[] ToArray() =>
            _ms.ToArray();

        public static int GetSize<T>()
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
                throw new InvalidOperationException("Data type 'char' is not supported.");
            return Unsafe.SizeOf<T>();
        }

        public static int GetArraySize<T>(T[] array)
            where T : unmanaged
        {
            return GetSize<int>() + (array.Length * GetSize<T>());
        }

        public static int GetStringSize(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            var strByteCount = s_utf8NoBom.GetByteCount(value);
            return GetSize<int>() + strByteCount;
        }
    }
}
