// Copyright (C) 2015-2024 The Neo Project.
//
// MemoryReader.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Neo.IO
{
    public ref struct MemoryReader
    {
        private readonly ReadOnlyMemory<byte> _memory;
        private readonly ReadOnlySpan<byte> _span;
        private int _pos = 0;

        public readonly int Position => _pos;

        public MemoryReader(ReadOnlyMemory<byte> memory)
        {
            _memory = memory;
            _span = memory.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void EnsurePosition(int move)
        {
            if (_pos + move > _span.Length) throw new FormatException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly byte Peek()
        {
            EnsurePosition(1);
            return _span[_pos];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean()
        {
            return ReadByte() switch
            {
                0 => false,
                1 => true,
                _ => throw new FormatException()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte()
        {
            EnsurePosition(1);
            var b = _span[_pos++];
            return unchecked((sbyte)b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            EnsurePosition(1);
            return _span[_pos++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            EnsurePosition(sizeof(short));
            var result = BinaryPrimitives.ReadInt16LittleEndian(_span[_pos..]);
            _pos += sizeof(short);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16BigEndian()
        {
            EnsurePosition(sizeof(short));
            var result = BinaryPrimitives.ReadInt16BigEndian(_span[_pos..]);
            _pos += sizeof(short);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            EnsurePosition(sizeof(ushort));
            var result = BinaryPrimitives.ReadUInt16LittleEndian(_span[_pos..]);
            _pos += sizeof(ushort);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16BigEndian()
        {
            EnsurePosition(sizeof(ushort));
            var result = BinaryPrimitives.ReadUInt16BigEndian(_span[_pos..]);
            _pos += sizeof(ushort);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            EnsurePosition(sizeof(int));
            var result = BinaryPrimitives.ReadInt32LittleEndian(_span[_pos..]);
            _pos += sizeof(int);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32BigEndian()
        {
            EnsurePosition(sizeof(int));
            var result = BinaryPrimitives.ReadInt32BigEndian(_span[_pos..]);
            _pos += sizeof(int);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            EnsurePosition(sizeof(uint));
            var result = BinaryPrimitives.ReadUInt32LittleEndian(_span[_pos..]);
            _pos += sizeof(uint);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32BigEndian()
        {
            EnsurePosition(sizeof(uint));
            var result = BinaryPrimitives.ReadUInt32BigEndian(_span[_pos..]);
            _pos += sizeof(uint);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            EnsurePosition(sizeof(long));
            var result = BinaryPrimitives.ReadInt64LittleEndian(_span[_pos..]);
            _pos += sizeof(long);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64BigEndian()
        {
            EnsurePosition(sizeof(long));
            var result = BinaryPrimitives.ReadInt64BigEndian(_span[_pos..]);
            _pos += sizeof(long);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            EnsurePosition(sizeof(ulong));
            var result = BinaryPrimitives.ReadUInt64LittleEndian(_span[_pos..]);
            _pos += sizeof(ulong);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64BigEndian()
        {
            EnsurePosition(sizeof(ulong));
            var result = BinaryPrimitives.ReadUInt64BigEndian(_span[_pos..]);
            _pos += sizeof(ulong);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadVarInt(ulong max = ulong.MaxValue)
        {
            var b = ReadByte();
            var value = b switch
            {
                0xfd => ReadUInt16(),
                0xfe => ReadUInt32(),
                0xff => ReadUInt64(),
                _ => b
            };
            if (value > max) throw new FormatException();
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadFixedString(int length)
        {
            EnsurePosition(length);
            var end = _pos + length;
            var i = _pos;
            while (i < end && _span[i] != 0) i++;
            var data = _span[_pos..i];
            for (; i < end; i++)
                if (_span[i] != 0)
                    throw new FormatException();
            _pos = end;
            return Utility.StrictUTF8.GetString(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadVarString(int max = 0x1000000)
        {
            var length = (int)ReadVarInt((ulong)max);
            EnsurePosition(length);
            var data = _span.Slice(_pos, length);
            _pos += length;
            return Utility.StrictUTF8.GetString(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> ReadMemory(int count)
        {
            EnsurePosition(count);
            var result = _memory.Slice(_pos, count);
            _pos += count;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> ReadVarMemory(int max = 0x1000000)
        {
            return ReadMemory((int)ReadVarInt((ulong)max));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> ReadToEnd()
        {
            var result = _memory[_pos..];
            _pos = _memory.Length;
            return result;
        }
    }
}
