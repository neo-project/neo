// Copyright (C) 2015-2026 The Neo Project.
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
        /// Performs integer division with ceiling (rounding up).
        /// Example: 10 / 3 = 4 instead of 3.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor (must be nonzero).</param>
        /// <returns>The result of division rounded up.</returns>
        /// <exception cref="ArgumentException">Thrown when divisor is zero.</exception>
        public static BigInteger DivideCeiling(this BigInteger dividend, BigInteger divisor)
        {
            // If it's 0, it will automatically throw DivideByZeroException
            var v = divisor > 0 ?
                BigInteger.DivRem(dividend, divisor, out var r) :
                BigInteger.DivRem(-dividend, -divisor, out r);

            if (r > 0)
                return v + BigInteger.One;

            return v;
        }

        /// <summary>
        /// Finds the lowest set bit in the specified value. If value is zero, returns -1.
        /// </summary>
        /// <param name="value">The value to find the lowest set bit in. The value.GetBitLength cannot greater than 2Gib.</param>
        /// <returns>The lowest set bit in the specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLowestSetBit(this BigInteger value)
        {
            if (value.Sign == 0) return -1; // special case for zero. TrailingZeroCount returns 32 in standard library.

            return (int)BigInteger.TrailingZeroCount(value);
        }

        /// <summary>
        /// Computes the remainder of the division of the specified value by the modulus.
        /// It's different from the `%` operator(see `BigInteger.Remainder`) if the dividend is negative.
        /// It always returns a non-negative value even if the dividend is negative.
        /// </summary>
        /// <param name="x">The value to compute the remainder of(i.e. dividend).</param>
        /// <param name="y">The modulus(i.e. divisor).</param>
        /// <returns>The remainder of the division of the specified value by the modulus.</returns>
        /// <exception cref="DivideByZeroException">Thrown when the divisor is zero.</exception>
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
        /// If the value is negative and index exceeds the bit length, it returns true.
        /// If the value is positive and index exceeds the bit length, it returns false.
        /// If index is negative, it returns false always.
        /// NOTE: the `value` is represented in sign-magnitude format,
        /// so it's different from the bit value in two's complement format(int, long).
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="index">The index of the bit to test.</param>
        /// <returns>True if the specified bit is set in the specified value, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TestBit(this BigInteger value, int index)
        {
            return !(value & (BigInteger.One << index)).IsZero;
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
        /// Converts a <see cref="BigInteger"/> to byte array in little-endian and eliminates all the leading zeros.
        /// If the value is zero, it returns an empty byte array.
        /// </summary>
        /// <param name="value">The <see cref="BigInteger"/> to convert.</param>
        /// <returns>The converted byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ToByteArrayStandard(this BigInteger value)
        {
            if (value.IsZero) return [];
            return value.ToByteArray();
        }

        /// <summary>
        /// Computes the square root of the specified value.
        /// </summary>
        /// <param name="value">The value to compute the square root of.</param>
        /// <returns>The square root of the specified value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the value is negative.</exception>
        public static BigInteger Sqrt(this BigInteger value)
        {
            if (value < 0) throw new InvalidOperationException($"value {value} can not be negative for '{nameof(Sqrt)}'.");
            if (value.IsZero) return BigInteger.Zero;
            if (value < 4) return BigInteger.One;

            var z = value;
            var x = BigInteger.One << (int)(((value - 1).GetBitLength() + 1) >> 1);
            while (x < z)
            {
                z = x;
                x = (value / x + x) / 2;
            }

            return z;
        }
    }
}
