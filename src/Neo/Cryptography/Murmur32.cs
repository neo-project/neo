// Copyright (C) 2015-2025 The Neo Project.
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
using System.Runtime.CompilerServices;

namespace Neo.Cryptography
{
    /// <summary>
    /// Computes the murmur hash for the input data.
    /// <remarks>Murmur32 is a non-cryptographic hash function.</remarks>
    /// </summary>
    public sealed class Murmur32
    {
        private const uint c1 = 0xcc9e2d51;
        private const uint c2 = 0x1b873593;
        private const int r1 = 15;
        private const int r2 = 13;
        private const uint m = 5;
        private const uint n = 0xe6546b64;

        private readonly uint _seed;
        private uint _hash;
        private int _length;

        public const int HashSizeInBits = 32;

        [Obsolete("Use HashSizeInBits")]
        public int HashSize => HashSizeInBits;

        /// <summary>
        /// Initializes a new instance of the <see cref="Murmur32"/> class with the specified seed.
        /// </summary>
        /// <param name="seed">The seed to be used.</param>
        public Murmur32(uint seed)
        {
            _seed = seed;
        }

        /// <summary>
        /// Append data to murmur computation
        /// </summary>
        /// <param name="source">Source</param>
        private void Append(ReadOnlySpan<byte> source)
        {
            _length += source.Length;
            for (; source.Length >= 4; source = source[4..])
            {
                var k = BinaryPrimitives.ReadUInt32LittleEndian(source);
                k *= c1;
                k = Helper.RotateLeft(k, r1);
                k *= c2;
                _hash ^= k;
                _hash = Helper.RotateLeft(_hash, r2);
                _hash = _hash * m + n;
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
                remainingBytes = Helper.RotateLeft(remainingBytes, r1);
                remainingBytes *= c2;
                _hash ^= remainingBytes;
            }
        }

        private uint GetCurrentHashUInt32()
        {
            var state = _hash ^ (uint)_length;
            state ^= state >> 16;
            state *= 0x85ebca6b;
            state ^= state >> 13;
            state *= 0xc2b2ae35;
            state ^= state >> 16;
            return state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize()
        {
            _hash = _seed;
            _length = 0;
        }

        /// <summary>
        /// Computes the murmur hash for the input data and resets the state.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <returns>The computed hash code in byte[4].</returns>
        public byte[] ComputeHash(ReadOnlySpan<byte> data)
        {
            var buffer = new byte[HashSizeInBits / 8];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, ComputeHashUInt32(data));
            return buffer;
        }

        /// <summary>
        /// Computes the murmur hash for the input data and resets the state.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <returns>The computed hash code in uint.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ComputeHashUInt32(ReadOnlySpan<byte> data)
        {
            Initialize();
            Append(data);
            return GetCurrentHashUInt32();
        }

        /// <summary>
        /// Computes the murmur hash for the input data.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the murmur algorithm.</param>
        /// <returns>The computed hash code in uint.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint HashToUInt32(ReadOnlySpan<byte> data, uint seed)
            => new Murmur32(seed).ComputeHashUInt32(data);
    }
}
