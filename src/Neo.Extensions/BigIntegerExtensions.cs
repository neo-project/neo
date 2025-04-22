// Copyright (C) 2015-2025 The Neo Project.
//
// BigIntegerExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
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
        /// <summary>
        /// Finds the lowest set bit in the specified value.
        /// </summary>
        /// <param name="value">The value to find the lowest set bit in.</param>
        /// <returns>The lowest set bit in the specified value.</returns>
        /// <exception cref="Exception">Thrown when the value is zero.</exception>
        public static int GetLowestSetBit(this BigInteger value)
        {
            if (value.Sign == 0)
                return -1;
            var b = value.ToByteArray();
            var w = 0;
            while (b[w] == 0)
                w++;
            for (var x = 0; x < 8; x++)
                if ((b[w] & 1 << x) > 0)
                    return x + w * 8;
            throw new Exception("The value is zero.");
        }

        /// <summary>
        /// Computes the remainder of the division of the specified value by the modulus.
        /// </summary>
        /// <param name="x">The value to compute the remainder of.</param>
        /// <param name="y">The modulus.</param>
        /// <returns>The remainder of the division of the specified value by the modulus.</returns>
        /// <exception cref="DivideByZeroException">Thrown when the modulus is zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BigInteger Mod(this BigInteger x, BigInteger y)
        {
            x %= y;
            if (x.Sign < 0)
                x += y;
            return x;
        }

        /// <summary>
        /// Computes the modular inverse of the specified value.
        /// </summary>
        /// <param name="value">The value to find the modular inverse of.</param>
        /// <param name="modulus">The modulus.</param>
        /// <returns>The modular inverse of the specified value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value or modulus is out of range.</exception>
        /// <exception cref="ArithmeticException">
        /// Thrown when no modular inverse exists for the given inputs. i.e. when the value and modulus are not coprime.
        /// </exception>
        public static BigInteger ModInverse(this BigInteger value, BigInteger modulus)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (modulus < 2) throw new ArgumentOutOfRangeException(nameof(modulus));

            BigInteger r = value, oldR = modulus, s = 1, oldS = 0;
            while (r > 0)
            {
                var q = oldR / r;
                (oldR, r) = (r, oldR % r);
                (oldS, s) = (s, oldS - q * s);
            }
            var result = oldS % modulus;
            if (result < 0) result += modulus;

            if (!(value * result % modulus).IsOne)
                throw new ArithmeticException("No modular inverse exists for the given inputs.");
            return result;
        }

        /// <summary>
        /// Tests whether the specified bit is set in the specified value.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="index">The index of the bit to test.</param>
        /// <returns>True if the specified bit is set in the specified value, otherwise false.</returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestBit(this BigInteger value, int index)
        {
            return (value & (BigInteger.One << index)) > BigInteger.Zero;
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
        /// <param name="value">The <see cref="BigInteger"/> to convert.</param>
        /// <returns>The converted byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToByteArrayStandard(this BigInteger value)
        {
            if (value.IsZero) return Array.Empty<byte>();
            return value.ToByteArray();
        }
    }
}
