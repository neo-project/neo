// Copyright (C) 2015-2025 The Neo Project.
//
// BigIntegerExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo.Extensions
{
    public static class BigIntegerExtensions
    {
        public static int GetLowestSetBit(this BigInteger i)
        {
            if (i.Sign == 0)
                return -1;
            var b = i.ToByteArray();
            var w = 0;
            while (b[w] == 0)
                w++;
            for (var x = 0; x < 8; x++)
                if ((b[w] & 1 << x) > 0)
                    return x + w * 8;
            throw new Exception();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BigInteger Mod(this BigInteger x, BigInteger y)
        {
            x %= y;
            if (x.Sign < 0)
                x += y;
            return x;
        }

        public static BigInteger ModInverse(this BigInteger a, BigInteger n)
        {
            if (BigInteger.GreatestCommonDivisor(a, n) != 1)
            {
                throw new ArithmeticException("No modular inverse exists for the given inputs.");
            }

            BigInteger i = n, v = 0, d = 1;
            while (a > 0)
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }
            v %= n;
            if (v < 0) v = (v + n) % n;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestBit(this BigInteger i, int index)
        {
            return (i & (BigInteger.One << index)) > BigInteger.Zero;
        }

        /// <summary>
        /// Finds the sum of the specified integers.
        /// </summary>
        /// <param name="source">The specified integers.</param>
        /// <returns>The sum of the integers.</returns>
        public static BigInteger Sum(this IEnumerable<BigInteger> source)
        {
            var sum = BigInteger.Zero;
            foreach (var bi in source) sum += bi;
            return sum;
        }

        /// <summary>
        /// Converts a <see cref="BigInteger"/> to byte array and eliminates all the leading zeros.
        /// </summary>
        /// <param name="i">The <see cref="BigInteger"/> to convert.</param>
        /// <returns>The converted byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToByteArrayStandard(this BigInteger i)
        {
            if (i.IsZero) return Array.Empty<byte>();
            return i.ToByteArray();
        }
    }
}
