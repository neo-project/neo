// Copyright (C) 2015-2024 The Neo Project.
//
// Murmur32.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace Neo.VM.Cryptography
{
    /// <summary>
    /// Computes the murmur hash for the input data.
    /// </summary>
    sealed class Murmur32 : HashAlgorithm
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
            length += cbSize;
            int remainder = cbSize & 3;
            int alignedLength = ibStart + (cbSize - remainder);
            for (int i = ibStart; i < alignedLength; i += 4)
            {
                uint k = BinaryPrimitives.ReadUInt32LittleEndian(array.AsSpan(i));
                k *= c1;
                k = BitOperations.RotateLeft(k, r1);
                k *= c2;
                hash ^= k;
                hash = BitOperations.RotateLeft(hash, r2);
                hash = hash * m + n;
            }
            if (remainder > 0)
            {
                uint remainingBytes = 0;
                switch (remainder)
                {
                    case 3: remainingBytes ^= (uint)array[alignedLength + 2] << 16; goto case 2;
                    case 2: remainingBytes ^= (uint)array[alignedLength + 1] << 8; goto case 1;
                    case 1: remainingBytes ^= array[alignedLength]; break;
                }
                remainingBytes *= c1;
                remainingBytes = BitOperations.RotateLeft(remainingBytes, r1);
                remainingBytes *= c2;
                hash ^= remainingBytes;
            }
        }

        protected override byte[] HashFinal()
        {
            hash ^= (uint)length;
            hash ^= hash >> 16;
            hash *= 0x85ebca6b;
            hash ^= hash >> 13;
            hash *= 0xc2b2ae35;
            hash ^= hash >> 16;

            byte[] buffer = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, hash);
            return buffer;
        }

        public override void Initialize()
        {
            hash = seed;
            length = 0;
        }
    }
}
