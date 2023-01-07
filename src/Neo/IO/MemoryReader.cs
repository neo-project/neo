// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
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
        private readonly ReadOnlyMemory<byte> memory;
        private readonly ReadOnlySpan<byte> span;
        private int pos = 0;

        public int Position => pos;

        public MemoryReader(ReadOnlyMemory<byte> memory)
        {
            this.memory = memory;
            this.span = memory.Span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsurePosition(int move)
        {
            if (pos + move > span.Length) throw new FormatException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Peek()
        {
            EnsurePosition(1);
            return span[pos];
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
            byte b = span[pos++];
            return unchecked((sbyte)b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            EnsurePosition(1);
            return span[pos++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            EnsurePosition(sizeof(short));
            var result = BinaryPrimitives.ReadInt16LittleEndian(span[pos..]);
            pos += sizeof(short);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16BigEndian()
        {
            EnsurePosition(sizeof(short));
            var result = BinaryPrimitives.ReadInt16BigEndian(span[pos..]);
            pos += sizeof(short);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            EnsurePosition(sizeof(ushort));
            var result = BinaryPrimitives.ReadUInt16LittleEndian(span[pos..]);
            pos += sizeof(ushort);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16BigEndian()
        {
            EnsurePosition(sizeof(ushort));
            var result = BinaryPrimitives.ReadUInt16BigEndian(span[pos..]);
            pos += sizeof(ushort);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            EnsurePosition(sizeof(int));
            var result = BinaryPrimitives.ReadInt32LittleEndian(span[pos..]);
            pos += sizeof(int);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32BigEndian()
        {
            EnsurePosition(sizeof(int));
            var result = BinaryPrimitives.ReadInt32BigEndian(span[pos..]);
            pos += sizeof(int);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            EnsurePosition(sizeof(uint));
            var result = BinaryPrimitives.ReadUInt32LittleEndian(span[pos..]);
            pos += sizeof(uint);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32BigEndian()
        {
            EnsurePosition(sizeof(uint));
            var result = BinaryPrimitives.ReadUInt32BigEndian(span[pos..]);
            pos += sizeof(uint);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            EnsurePosition(sizeof(long));
            var result = BinaryPrimitives.ReadInt64LittleEndian(span[pos..]);
            pos += sizeof(long);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64BigEndian()
        {
            EnsurePosition(sizeof(long));
            var result = BinaryPrimitives.ReadInt64BigEndian(span[pos..]);
            pos += sizeof(long);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            EnsurePosition(sizeof(ulong));
            var result = BinaryPrimitives.ReadUInt64LittleEndian(span[pos..]);
            pos += sizeof(ulong);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64BigEndian()
        {
            EnsurePosition(sizeof(ulong));
            var result = BinaryPrimitives.ReadUInt64BigEndian(span[pos..]);
            pos += sizeof(ulong);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadVarInt(ulong max = ulong.MaxValue)
        {
            byte b = ReadByte();
            ulong value = b switch
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
            int end = pos + length;
            int i = pos;
            while (i < end && span[i] != 0) i++;
            ReadOnlySpan<byte> data = span[pos..i];
            for (; i < end; i++)
                if (span[i] != 0)
                    throw new FormatException();
            pos = end;
            return Utility.StrictUTF8.GetString(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadVarString(int max = 0x1000000)
        {
            int length = (int)ReadVarInt((ulong)max);
            EnsurePosition(length);
            ReadOnlySpan<byte> data = span.Slice(pos, length);
            pos += length;
            return Utility.StrictUTF8.GetString(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> ReadMemory(int count)
        {
            EnsurePosition(count);
            ReadOnlyMemory<byte> result = memory.Slice(pos, count);
            pos += count;
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
            ReadOnlyMemory<byte> result = memory[pos..];
            pos = memory.Length;
            return result;
        }
    }
}
