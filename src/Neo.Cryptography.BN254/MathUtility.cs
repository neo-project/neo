// Copyright (C) 2015-2025 The Neo Project.
//
// MathUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;

namespace Neo.Cryptography.BN254
{
    static class MathUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (ulong low, ulong high) Mac(ulong z, ulong x, ulong y, ulong carry)
        {
            // Multiply-accumulate: z = z + x * y + carry
            ulong hi, lo;
            MultiplyUInt64(x, y, out hi, out lo);
            
            lo += carry;
            hi += lo < carry ? 1UL : 0UL;
            
            lo += z;
            hi += lo < z ? 1UL : 0UL;
            
            return (lo, hi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (ulong result, ulong carry) Mac(ulong z, ulong x, ulong y, ulong a, ulong carry)
        {
            // Extended multiply-accumulate: z = z + x * y + a + carry
            var (lo, hi) = Mac(z, x, y, carry);
            lo += a;
            hi += lo < a ? 1UL : 0UL;
            return (lo, hi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (ulong result, ulong borrow) Sbb(ulong x, ulong y, ulong borrow)
        {
            // Subtract with borrow
            ulong result = x - y - borrow;
            ulong newBorrow = ((x < y) || (x == y && borrow != 0)) ? 1UL : 0UL;
            return (result, newBorrow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (ulong result, ulong carry) Adc(ulong x, ulong y, ulong carry)
        {
            // Add with carry
            ulong result = x + y + carry;
            ulong newCarry = ((result < x) || (result == x && carry != 0)) ? 1UL : 0UL;
            return (result, newCarry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MultiplyUInt64(ulong x, ulong y, out ulong hi, out ulong lo)
        {
            // 64-bit multiplication producing 128-bit result
            ulong x0 = x & 0xFFFFFFFF;
            ulong x1 = x >> 32;
            ulong y0 = y & 0xFFFFFFFF;
            ulong y1 = y >> 32;

            ulong p00 = x0 * y0;
            ulong p01 = x0 * y1;
            ulong p10 = x1 * y0;
            ulong p11 = x1 * y1;

            ulong middle = p01 + p10;
            ulong carry = middle < p01 ? 1UL : 0UL;

            lo = p00 + (middle << 32);
            hi = p11 + (middle >> 32) + (carry << 32) + (lo < p00 ? 1UL : 0UL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ConditionalSelect(byte a, byte b, bool condition)
        {
            return (byte)(a ^ ((a ^ b) & (condition ? 0xFF : 0)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ConditionalSelect(ulong a, ulong b, bool condition)
        {
            return a ^ ((a ^ b) & (condition ? ulong.MaxValue : 0));
        }
    }
}