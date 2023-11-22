// Copyright (C) 2016-2023 The Neo Project.
//
// The neo-vm is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Neo.VM
{
    internal static class Utility
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

#if !NET5_0_OR_GREATER
        static int GetBitLength(this BigInteger i)
        {
            byte[] b = i.ToByteArray();
            return (b.Length - 1) * 8 + BitLen(i.Sign > 0 ? b[b.Length - 1] : 255 - b[b.Length - 1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int BitLen(int w)
        {
            return (w < 1 << 15 ? (w < 1 << 7
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
                : (w < 1 << 29 ? (w < 1 << 28 ? 28 : 29) : (w < 1 << 30 ? 30 : 31)))));
        }
#endif
    }
}
