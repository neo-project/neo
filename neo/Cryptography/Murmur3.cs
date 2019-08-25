using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    public sealed class Murmur3 : HashAlgorithm
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

        public Murmur3(uint seed)
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
                uint k = array.ToUInt32(i);
                k *= c1;
                k = RotateLeft(k, r1);
                k *= c2;
                hash ^= k;
                hash = RotateLeft(hash, r2);
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
                remainingBytes = RotateLeft(remainingBytes, r1);
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
            return BitConverter.GetBytes(hash);
        }

        public override void Initialize()
        {
            hash = seed;
            length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateLeft(uint x, byte n)
        {
            return (x << n) | (x >> (32 - n));
        }
    }
}
