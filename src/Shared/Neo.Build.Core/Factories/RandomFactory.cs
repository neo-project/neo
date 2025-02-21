// Copyright (C) 2015-2025 The Neo Project.
//
// RandomFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers.Binary;

namespace Neo.Build.Core.Factories
{
    public static class RandomFactory
    {
        private static readonly Func<int> s_randomSeedFactory = static () => unchecked((int)(DateTimeOffset.UtcNow.Ticks >> 28));

        public static Random Shared { get; } = new(s_randomSeedFactory());

        public static readonly Func<Random> CreateNew = static () => new(s_randomSeedFactory());

        public static readonly Func<byte, byte, byte> NextByte = static (min, max) => unchecked((byte)CreateNew().Next(min, max));

        public static readonly Func<short, short, short> NextInt16 = static (min, max) => unchecked((short)CreateNew().Next(min, max));

        public static readonly Func<ushort, ushort, ushort> NextUInt16 = static (min, max) => unchecked((ushort)CreateNew().Next(min, max));

        public static readonly Func<uint> NextUInt32 = static () =>
        {
            Span<byte> longBytes = stackalloc byte[4];
            CreateNew().NextBytes(longBytes);
            return BinaryPrimitives.ReadUInt32LittleEndian(longBytes);
        };

        public static readonly Func<long> NextInt64 = static () =>
        {
            Span<byte> longBytes = stackalloc byte[8];
            CreateNew().NextBytes(longBytes);
            return BinaryPrimitives.ReadInt64LittleEndian(longBytes);
        };

        public static readonly Func<ulong> NextUInt64 = static () =>
        {
            Span<byte> longBytes = stackalloc byte[8];
            CreateNew().NextBytes(longBytes);
            return BinaryPrimitives.ReadUInt64LittleEndian(longBytes);
        };
    }
}
