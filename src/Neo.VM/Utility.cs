// Copyright (C) 2015-2024 The Neo Project.
//
// Utility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Numerics;
using System.Text;

namespace Neo.VM
{
    public static class Utility
    {
        public static Encoding StrictUTF8 { get; }

        static Utility()
        {
            StrictUTF8 = (Encoding)Encoding.UTF8.Clone();
            StrictUTF8.DecoderFallback = DecoderFallback.ExceptionFallback;
            StrictUTF8.EncoderFallback = EncoderFallback.ExceptionFallback;
        }

        public static BigInteger ModInverse(this BigInteger value, BigInteger modulus)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (modulus < 2) throw new ArgumentOutOfRangeException(nameof(modulus));
            BigInteger r = value, old_r = modulus, s = 1, old_s = 0;
            while (r > 0)
            {
                BigInteger q = old_r / r;
                (old_r, r) = (r, old_r % r);
                (old_s, s) = (s, old_s - q * s);
            }
            BigInteger result = old_s % modulus;
            if (result < 0) result += modulus;
            if (!(value * result % modulus).IsOne) throw new InvalidOperationException();
            return result;
        }

        public static BigInteger Sqrt(this BigInteger value)
        {
            if (value < 0) throw new InvalidOperationException("value can not be negative");
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
        /// </summary>
        /// <returns>The minimum non-negative number of bits in two's complement notation without the sign bit.</returns>
        /// <remarks>This method returns 0 if the value of current object is equal to <see cref="BigInteger.Zero"/> or <see cref="BigInteger.MinusOne"/>. For positive integers the return value is equal to the ordinary binary representation string length.</remarks>
        public static long GetBitLength(this BigInteger num)
        {
            if (num.IsZero) return 0;

            var bytes = num.ToByteArray();
            var size = bytes.Length;

            if (size == 0) return 0;
            if (num.Sign >= 0)
            {
                var v = bytes[size - 1]; // 8-bit value to find the log2 of 
                if (v == 0) return (size - 1) * 8;
                var r = (v > 0xF) ? 4 : 0; // result of log2(v) will go here
                v >>= r;
                var shift = (v > 0x3) ? 2 : 0;
                v >>= shift;
                r |= shift;
                r |= v >> 1;
                var bitLength = (size - 1) * 8 + r + 1;
                return bitLength;
            }
            else
            {
                // TODO: Fix negative numbers
                throw new InvalidOperationException("value can not be negative");
            }
        }
    }
}
