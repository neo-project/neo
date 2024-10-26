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

namespace Neo.IO.Buffers
{
    internal class MemoryBuffer : IDisposable
    {
        private static readonly UTF8Encoding s_utf8NoBom = new(false, true);

        private readonly Stream _stream;

        public void Dispose()
        {
            _stream.Dispose();
        }

        public MemoryBuffer(Stream stream)
        {
            _stream = stream;
        }

        public void WriteRaw(byte[] buffer) =>
            _stream.Write(buffer, 0, buffer.Length);

        public void ReadRaw(byte[] buffer) =>
            _stream.Read(buffer, 0, buffer.Length);

        public void Write<T>(T value)
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
                throw new InvalidOperationException("Data type 'char' is not supported.");

            var size = Unsafe.SizeOf<T>();
            var buffer = new byte[size];

            Unsafe.As<byte, T>(ref buffer[0]) = value;

            _stream.Write(buffer, 0, size);
        }

        public void WriteArray<T>(T[] array)
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
                throw new InvalidOperationException("Data type 'char' is not supported.");

            Write(array.Length);
            foreach (var item in array)
                Write(item);
        }

        public void WriteString(string value)
        {
            var strByteCount = s_utf8NoBom.GetByteCount(value);
            Write(strByteCount);

            var buffer = s_utf8NoBom.GetBytes(value);
            _stream.Write(buffer, 0, buffer.Length);
        }

        public T Read<T>()
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
                throw new InvalidOperationException("Data type 'char' is not supported.");

            var size = Unsafe.SizeOf<T>();
            var buffer = new byte[size];

            _stream.Read(buffer, 0, size);

            return Unsafe.As<byte, T>(ref buffer[0]);
        }

        public T[] ReadArray<T>()
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
                throw new InvalidOperationException("Data type 'char' is not supported.");

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
            _stream.Read(buffer, 0, strByteCount);
            return s_utf8NoBom.GetString(buffer);
        }

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
            if (typeof(T) == typeof(char))
                throw new InvalidOperationException("Data type 'char' is not supported.");

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
