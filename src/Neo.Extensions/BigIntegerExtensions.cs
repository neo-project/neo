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

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Neo;

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

    internal static int TrailingZeroCount(byte[] b)
    {
        var w = 0;
        while (b[w] == 0) w++;
        for (var x = 0; x < 8; x++)
        {
            if ((b[w] & 1 << x) > 0)
                return x + w * 8; // cannot greater than 2Gib
        }
        return -1; // unreachable, because returned earlier if value is zero
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        ArgumentOutOfRangeException.ThrowIfLessThan(modulus, 2);

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
        return value.GetBitLength();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int BitCount(byte w)
    {
        return w < (1 << 4) // 16
            ? (w < (1 << 2) // 4
                ? (w < (1 << 1) ? w : 2)  // 2
                : (w < (1 << 3) ? 3 : 4)) // 8
            : (w < (1 << 6) // 64
                ? (w < (1 << 5) ? 5 : 6)   // 32
                : (w < (1 << 7) ? 7 : 8)); // 128
    }
}
