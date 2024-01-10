// Copyright (C) 2015-2024 The Neo Project.
//
// MathUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Cryptography.BLS12_381;

static class MathUtility
{
    public static (ulong result, ulong carry) Adc(ulong a, ulong b, ulong carry)
    {
        ulong result = unchecked(a + b + carry);
        carry = ((a & b) | ((a | b) & (~result))) >> 63;
        return (result, carry);
    }

    public static (ulong result, ulong borrow) Sbb(ulong a, ulong b, ulong borrow)
    {
        ulong result = unchecked(a - b - borrow);
        borrow = (((~a) & b) | (~(a ^ b)) & result) >> 63;
        return (result, borrow);
    }

    public static (ulong low, ulong high) Mac(ulong z, ulong x, ulong y, ulong carry)
    {
        ulong high = BigMul(x, y, out ulong low);
        (low, carry) = Adc(low, carry, 0);
        (high, _) = Adc(high, 0, carry);
        (low, carry) = Adc(low, z, 0);
        (high, _) = Adc(high, 0, carry);
        return (low, high);
    }

    /// <summary>Produces the full product of two unsigned 64-bit numbers.</summary>
    /// <param name="a">The first number to multiply.</param>
    /// <param name="b">The second number to multiply.</param>
    /// <param name="low">The low 64-bit of the product of the specified numbers.</param>
    /// <returns>The high 64-bit of the product of the specified numbers.</returns>
    public static ulong BigMul(ulong a, ulong b, out ulong low)
    {
        // Adaptation of algorithm for multiplication
        // of 32-bit unsigned integers described
        // in Hacker's Delight by Henry S. Warren, Jr. (ISBN 0-201-91465-4), Chapter 8
        // Basically, it's an optimized version of FOIL method applied to
        // low and high dwords of each operand

        // Use 32-bit uints to optimize the fallback for 32-bit platforms.
        uint al = (uint)a;
        uint ah = (uint)(a >> 32);
        uint bl = (uint)b;
        uint bh = (uint)(b >> 32);

        ulong mull = ((ulong)al) * bl;
        ulong t = ((ulong)ah) * bl + (mull >> 32);
        ulong tl = ((ulong)al) * bh + (uint)t;

        low = tl << 32 | (uint)mull;

        return ((ulong)ah) * bh + (t >> 32) + (tl >> 32);
    }
}
