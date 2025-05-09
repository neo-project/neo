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
            if (value.IsZero) return [];
            return value.ToByteArray();
        }

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

        /// <summary>
        /// Gets the number of bits required for shortest two's complement representation of the current instance without the sign bit.
        /// Note: This method is imprecise and might not work as expected with integers larger than 256 bits if less than .NET5.
        /// </summary>
        /// <returns>The minimum non-negative number of bits in two's complement notation without the sign bit.</returns>
        /// <remarks>
        /// This method returns 0 if the value of current object is equal to
        /// <see cref="BigInteger.Zero"/> or <see cref="BigInteger.MinusOne"/>.
        /// For positive integers the return value is equal to the ordinary binary representation string length.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GetBitLength(this BigInteger value)
        {
#if NET5_0_OR_GREATER
            return value.GetBitLength();
#else
            return BitLength(value);
#endif
        }

        /// <summary>
        /// GetBitLength for earlier than .NET5.0
        /// </summary>
        internal static long BitLength(this BigInteger value)
        {
            if (value == 0 || value == BigInteger.MinusOne) return 0;

            var b = value.ToByteArray();
            if (b.Length == 1 || (b.Length == 2 && b[1] == 0))
            {
                return BitCount(value.Sign > 0 ? b[0] : (byte)(255 - b[0]));
            }
            return (b.Length - 1) * 8 + BitCount(value.Sign > 0 ? b[^1] : (byte)(255 - b[^1]));
        }

        private static int BitCount(int w)
        {
            return w < 1 << 15 ? (w < 1 << 7
                ? (w < 1 << 3 ? (w < 1 << 1
                ? (w < 1 << 0 ? (w < 0 ? 32 : 0) : 1)
                : (w < 1 << 2 ? 2 : 3)) : (w < 1 << 5
                ? (w < 1 << 4 ? 4 : 5)
                : (w < 1 << 6 ? 6 : 7)))
                : (w < 1 << 11
                ? (w < 1 << 9 ? (w < 1 << 8 ? 8 : 9) : (w < 1 << 10 ? 10 : 11))
                : (w < 1 << 13 ? (w < 1 << 12 ? 12 : 13) : (w < 1 << 14 ? 14 : 15)))) : (w < 1 << 23 ? (w < 1 << 19
                ? (w < 1 << 17 ? (w < 1 << 16 ? 16 : 17) : (w < 1 << 18 ? 18 : 19))
                : (w < 1 << 21 ? (w < 1 << 20 ? 20 : 21) : (w < 1 << 22 ? 22 : 23))) : (w < 1 << 27
                ? (w < 1 << 25 ? (w < 1 << 24 ? 24 : 25) : (w < 1 << 26 ? 26 : 27))
                : (w < 1 << 29 ? (w < 1 << 28 ? 28 : 29) : (w < 1 << 30 ? 30 : 31))));
        }
    }
}
