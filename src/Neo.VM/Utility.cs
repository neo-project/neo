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
                var q = old_r / r;
                (old_r, r) = (r, old_r % r);
                (old_s, s) = (s, old_s - q * s);
            }
            var result = old_s % modulus;
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

        public static bool Is256Bit(this BigInteger bigInt)
        {
            byte[] bytes = bigInt.ToByteArray();
            int bitLength = 0;
            bool foundSignificantBit = false;

            // Iterate over each byte from the most significant byte.
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                byte b = bytes[i];
                for (int bit = 7; bit >= 0; bit--)
                {
                    bool isBitSet = (b & (1 << bit)) != 0;

                    // For positive numbers, find the first set bit.
                    // For negative numbers (two's complement), find the first unset bit.
                    if (bigInt.Sign > 0 && isBitSet || bigInt.Sign < 0 && !isBitSet)
                    {
                        bitLength = i * 8 + bit + 1;
                        foundSignificantBit = true;
                        break;
                    }
                }
                if (foundSignificantBit)
                {
                    break;
                }
            }

            // Check if the calculated bit length is within 256 bits.
            return bitLength <= 256;
        }

        /// <summary>
        /// Gets the number of bits required for shortest two's complement representation of the current instance without the sign bit.
        /// </summary>
        /// <returns>The minimum non-negative number of bits in two's complement notation without the sign bit.</returns>
        /// <remarks>This method returns 0 if the value of current object is equal to <see cref="BigInteger.Zero"/> or <see cref="BigInteger.MinusOne"/>. For positive integers the return value is equal to the ordinary binary representation string length.</remarks>
        public static long GetBitLength(this BigInteger value)
        {
            if (value.IsZero || value == BigInteger.MinusOne)
                return 0;

            if (!value.Is256Bit())
                throw new InvalidOperationException();

            return (long)Math.Ceiling(BigInteger.Log(value.Sign < 0 ? -value : value + 1, 2.0));
        }
    }
}
