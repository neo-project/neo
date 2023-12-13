// Copyright (C) 2015-2022 The Neo Project.
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
using System.Numerics;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    /// <summary>
    /// Computes the murmur hash for the input data.
    /// </summary>
    public sealed class Murmur32 : HashAlgorithm
    {
        private const uint c1 = 0xcc9e2d51;
        private const uint c2 = 0x1b873593;
        private const int r1 = 15;
        private const int r2 = 13;
        private const uint m = 5;
        private const uint n = 0xe6546b64;

        private readonly uint seed;
        private uint hash;
        private int length;

        public override int HashSize => 32;

        /// <summary>
        /// Initializes a new instance of the <see cref="Murmur32"/> class with the specified seed.
        /// </summary>
        /// <param name="seed">The seed to be used.</param>
        public Murmur32(uint seed)
        {
            this.seed = seed;
            Initialize();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            HashCore(array.AsSpan(ibStart, cbSize));
        }

        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            length += source.Length;
            for (; source.Length >= 4; source = source[4..])
            {
                uint k = BinaryPrimitives.ReadUInt32LittleEndian(source);
                k *= c1;
                k = BitOperations.RotateLeft(k, r1);
                k *= c2;
                hash ^= k;
                hash = BitOperations.RotateLeft(hash, r2);
                hash = hash * m + n;
            }
            if (source.Length > 0)
            {
                uint remainingBytes = 0;
                switch (source.Length)
                {
                    case 3: remainingBytes ^= (uint)source[2] << 16; goto case 2;
                    case 2: remainingBytes ^= (uint)source[1] << 8; goto case 1;
                    case 1: remainingBytes ^= source[0]; break;
                }
                remainingBytes *= c1;
                remainingBytes = BitOperations.RotateLeft(remainingBytes, r1);
                remainingBytes *= c2;
                hash ^= remainingBytes;
            }
        }

        protected override byte[] HashFinal()
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(uint));
            TryHashFinal(buffer, out _);
            return buffer;
        }

        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            hash ^= (uint)length;
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;

            bytesWritten = Math.Min(destination.Length, sizeof(uint));
            return BinaryPrimitives.TryWriteUInt32LittleEndian(destination, hash);
        }

        public override void Initialize()
        {
            hash = seed;
            length = 0;
        }
    }
}
