// Copyright (C) 2015-2024 The Neo Project.
//
// ByteArrayBuffer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.Hosting.App.Buffers
{
    internal sealed class ByteArrayBuffer : IEnumerable<byte>
    {
        private static readonly UTF8Encoding s_utf8NoBom = new(false, true);

        private byte[] _data;

        public int Position { get; set; }

        public ByteArrayBuffer()
        {
            _data = [];
            Position = 0;
        }

        public ByteArrayBuffer(byte[] buffer)
        {
            _data = buffer;
        }

        public static int GetStringSize(string value) =>
            s_utf8NoBom.GetByteCount(value) + sizeof(int);

        public ByteArrayBuffer Write<T>(T value)
            where T : unmanaged
        {
            var typeSize = Unsafe.SizeOf<T>();

            if (Position + typeSize > _data.Length)
                Array.Resize(ref _data, _data.Length + typeSize);

            Unsafe.As<byte, T>(ref _data[Position]) = value;

            Position += typeSize;
            return this;
        }

        public ByteArrayBuffer Write<T>(T[] values)
            where T : unmanaged
        {
            Write(values.Length);
            foreach (var value in values)
                Write(value);
            return this;
        }

        public ByteArrayBuffer Write(string value)
        {
            var strByteCount = s_utf8NoBom.GetByteCount(value);
            Write(strByteCount);

            if (Position + strByteCount > _data.Length)
                Array.Resize(ref _data, _data.Length + strByteCount);

            Position += s_utf8NoBom.GetBytes(value, _data.AsSpan(Position));
            return this;
        }

        public T Read<T>()
            where T : unmanaged
        {
            var typeSize = Unsafe.SizeOf<T>();

            if (Position + typeSize > _data.Length)
                throw new IndexOutOfRangeException();

            var value = Unsafe.As<byte, T>(ref _data[Position]);
            Position += typeSize;

            return value;
        }

        public T[] ReadArray<T>()
            where T : unmanaged
        {
            var length = Read<int>();
            var values = new T[length];
            for (var i = 0; i < length; i++)
                values[i] = Read<T>();
            return values;
        }

        public string ReadString()
        {
            var strByteCount = Read<int>();

            if (Position + strByteCount > _data.Length)
                throw new IndexOutOfRangeException();

            var value = s_utf8NoBom.GetString(_data, Position, strByteCount);
            Position += strByteCount;

            return value;
        }

        #region IEnumerable<byte>

        public IEnumerator<byte> GetEnumerator()
        {
            foreach (var b in _data)
                yield return b;
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        #endregion
    }
}
