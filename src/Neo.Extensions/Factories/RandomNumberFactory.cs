// Copyright (C) 2015-2025 The Neo Project.
//
// RandomNumberFactory.cs file belongs to the neo project and is free
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

namespace Neo.Extensions.Factories
{
    public static class RandomNumberFactory
    {
        public static sbyte NextSByte() =>
            NextSByte(sbyte.MinValue, sbyte.MaxValue);

        public static sbyte NextSByte(sbyte maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue));

            return NextSByte(sbyte.MinValue, maxValue);
        }

        public static sbyte NextSByte(sbyte minValue, sbyte maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            return (sbyte)(NextUInt32((uint)(maxValue - minValue)) + minValue);
        }

        public static byte NextByte() =>
            NextByte(byte.MaxValue, byte.MaxValue);

        public static byte NextByte(byte maxValue) =>
            NextByte(byte.MinValue, maxValue);

        public static byte NextByte(byte minValue, byte maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            return (byte)(NextUInt32((uint)(maxValue - minValue)) + minValue);
        }

        public static short NextInt16() =>
            NextInt16(0, short.MaxValue);

        public static short NextInt16(short maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue));

            return NextInt16(0, maxValue);
        }

        public static short NextInt16(short minValue, short maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            return (short)(NextUInt32((uint)(maxValue - minValue)) + minValue);
        }

        public static ushort NextUInt16() =>
            NextUInt16(0, ushort.MaxValue);

        public static ushort NextUInt16(ushort maxValue) =>
            NextUInt16(0, maxValue);

        public static ushort NextUInt16(ushort minValue, ushort maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            return (ushort)(NextUInt32((uint)(maxValue - minValue)) + minValue);
        }

        public static int NextInt32() =>
            NextInt32(0, int.MaxValue);

        public static int NextInt32(int maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue));

            return NextInt32(0, maxValue);
        }

        public static int NextInt32(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            return (int)NextUInt32((uint)(maxValue - minValue)) + minValue;
        }

        public static uint NextUInt32()
        {
            Span<byte> longBytes = stackalloc byte[4];
            RandomNumberGenerator.Fill(longBytes);
            return BinaryPrimitives.ReadUInt32LittleEndian(longBytes);
        }

        public static uint NextUInt32(uint maxValue)
        {
            var randomProduct = (ulong)maxValue * NextUInt32();
            var lowPart = (uint)randomProduct;

            if (lowPart < maxValue)
            {
                var remainder = (0u - maxValue) % maxValue;

                while (lowPart < remainder)
                {
                    randomProduct = (ulong)maxValue * NextUInt32();
                    lowPart = (uint)randomProduct;
                }
            }

            return (uint)(randomProduct >> 32);
        }

        public static uint NextUInt32(uint minValue, uint maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            return NextUInt32(maxValue - minValue) + minValue;
        }

        public static long NextInt64() =>
            NextInt64(0, long.MaxValue);

        public static long NextInt64(long maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue));

            return NextInt64(0, maxValue);
        }

        public static long NextInt64(long minValue, long maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            return (long)NextUInt64((ulong)(maxValue - minValue)) + minValue;
        }

        public static ulong NextUInt64()
        {
            Span<byte> longBytes = stackalloc byte[8];
            RandomNumberGenerator.Fill(longBytes);
            return BinaryPrimitives.ReadUInt64LittleEndian(longBytes);
        }

        public static ulong NextUInt64(ulong maxValue)
        {
            var randomProduct = BigMul(maxValue, NextUInt64(), out var lowPart);

            if (lowPart < maxValue)
            {
                var remainder = (0ul - maxValue) % maxValue;

                while (lowPart < remainder)
                {
                    randomProduct = BigMul(maxValue, NextUInt64(), out lowPart);
                }
            }

            return randomProduct;
        }

        public static ulong NextUInt64(ulong minValue, ulong maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            return NextUInt64(maxValue - minValue) + minValue;
        }

        public static BigInteger NextBigInteger(int sizeInBits)
        {
            if (sizeInBits < 0)
                throw new ArgumentException("sizeInBits must be non-negative.");

            if (sizeInBits == 0)
                return BigInteger.Zero;

            Span<byte> b = stackalloc byte[sizeInBits / 8 + 1];
            RandomNumberGenerator.Fill(b);

            if (sizeInBits % 8 == 0)
                b[^1] = 0;
            else
                b[^1] &= (byte)((1 << sizeInBits % 8) - 1);

            return new BigInteger(b);
        }

        private static ulong BigMul(ulong a, ulong b, out ulong low)
        {
            // Adaptation of algorithm for multiplication
            // of 32-bit unsigned integers described
            // in Hacker's Delight by Henry S. Warren, Jr. (ISBN 0-201-91465-4), Chapter 8
            // Basically, it's an optimized version of FOIL method applied to
            // low and high dwords of each operand

            // Use 32-bit uints to optimize the fallback for 32-bit platforms.
            var al = (uint)a;
            var ah = (uint)(a >> 32);
            var bl = (uint)b;
            var bh = (uint)(b >> 32);

            var mull = ((ulong)al) * bl;
            var t = ((ulong)ah) * bl + (mull >> 32);
            var tl = ((ulong)al) * bh + (uint)t;

            low = (tl << 32) | (uint)mull;

            return ((ulong)ah) * bh + (t >> 32) + (tl >> 32);
        }
    }
}
