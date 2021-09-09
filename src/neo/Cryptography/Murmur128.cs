// Copyright (C) 2015-2021 The Neo Project.
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
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    /// <summary>
    /// Computes the 128 bits murmur hash for the input data.
    /// </summary>
    public sealed class Murmur128 : HashAlgorithm
    {
        private const ulong c1 = 0x87c37b91114253d5;
        private const ulong c2 = 0x4cf5ad432745937f;
        private const int r1 = 31;
        private const int r2 = 33;
        private const uint m = 5;
        private const uint n1 = 0x52dce729;
        private const uint n2 = 0x38495ab5;

        private readonly uint seed;
        private int length;

        public override int HashSize => 128;

        private ulong H1 { get; set; }
        private ulong H2 { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Murmur128"/> class with the specified seed.
        /// </summary>
        /// <param name="seed">The seed to be used.</param>
        public Murmur128(uint seed)
        {
            this.seed = seed;
            Initialize();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            length += cbSize;
            int remainder = cbSize & 15;
            int alignedLength = ibStart + (cbSize - remainder);
            for (int i = ibStart; i < alignedLength; i += 16)
            {
                ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(array.AsSpan(i));
                k1 *= c1;
                k1 = BitOperations.RotateLeft(k1, r1);
                k1 *= c2;
                H1 ^= k1;
                H1 = BitOperations.RotateLeft(H1, 27);
                H1 += H2;
                H1 = H1 * m + n1;

                ulong k2 = BinaryPrimitives.ReadUInt64LittleEndian(array.AsSpan(i + 8));
                k2 *= c2;
                k2 = BitOperations.RotateLeft(k2, r2);
                k2 *= c1;
                H2 ^= k2;
                H2 = BitOperations.RotateLeft(H2, 31);
                H2 += H1;
                H2 = H2 * m + n2;
            }

            if (remainder > 0)
            {
                ulong remainingBytesL = 0, remainingBytesH = 0;
                switch (remainder)
                {
                    case 15: remainingBytesH ^= (ulong)array[alignedLength + 14] << 48; goto case 14;
                    case 14: remainingBytesH ^= (ulong)array[alignedLength + 13] << 40; goto case 13;
                    case 13: remainingBytesH ^= (ulong)array[alignedLength + 12] << 32; goto case 12;
                    case 12: remainingBytesH ^= (ulong)array[alignedLength + 11] << 24; goto case 11;
                    case 11: remainingBytesH ^= (ulong)array[alignedLength + 10] << 16; goto case 10;
                    case 10: remainingBytesH ^= (ulong)array[alignedLength + 9] << 8; goto case 9;
                    case 9: remainingBytesH ^= (ulong)array[alignedLength + 8] << 0; goto case 8;
                    case 8: remainingBytesL ^= (ulong)array[alignedLength + 7] << 56; goto case 7;
                    case 7: remainingBytesL ^= (ulong)array[alignedLength + 6] << 48; goto case 6;
                    case 6: remainingBytesL ^= (ulong)array[alignedLength + 5] << 40; goto case 5;
                    case 5: remainingBytesL ^= (ulong)array[alignedLength + 4] << 32; goto case 4;
                    case 4: remainingBytesL ^= (ulong)array[alignedLength + 3] << 24; goto case 3;
                    case 3: remainingBytesL ^= (ulong)array[alignedLength + 2] << 16; goto case 2;
                    case 2: remainingBytesL ^= (ulong)array[alignedLength + 1] << 8; goto case 1;
                    case 1: remainingBytesL ^= (ulong)array[alignedLength] << 0; break;
                }

                H2 ^= BitOperations.RotateLeft(remainingBytesH * c2, r2) * c1;
                H1 ^= BitOperations.RotateLeft(remainingBytesL * c1, r1) * c2;
            }
        }

        protected override byte[] HashFinal()
        {
            ulong len = (ulong)length;
            H1 ^= len; H2 ^= len;

            H1 += H2;
            H2 += H1;

            H1 = FMix(H1);
            H2 = FMix(H2);

            H1 += H2;
            H2 += H1;

            var buffer = new byte[sizeof(ulong) * 2];
            Span<byte> bytes = buffer;

            BinaryPrimitives.WriteUInt64LittleEndian(bytes, H1);
            BinaryPrimitives.WriteUInt64LittleEndian(bytes[sizeof(ulong)..], H2);

            return buffer;
        }

        public override void Initialize()
        {
            H1 = H2 = seed;
            length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong FMix(ulong h)
        {
            h = (h ^ (h >> 33)) * 0xff51afd7ed558ccd;
            h = (h ^ (h >> 33)) * 0xc4ceb9fe1a85ec53;

            return (h ^ (h >> 33));
        }
    }
}
