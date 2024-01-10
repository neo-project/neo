// Copyright (C) 2015-2024 The Neo Project.
//
// BigIntegerExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#if !NET5_0_OR_GREATER

namespace System.Numerics
{
    public static class BigIntegerExtensions
    {
        public static long GetBitLength(this BigInteger num)
        {
            var bytes = num.ToByteArray();
            var size = bytes.Length;
            if (size == 0) return 0;
            int v = bytes[size - 1]; // 8-bit value to find the log2 of 
            if (v == 0) return (size - 1) * 8;
            int r; // result of log2(v) will go here
            int shift;
            r = (v > 0xF) ? 4 : 0; v >>= r;
            shift = (v > 0x3) ? 2 : 0; v >>= shift; r |= shift;
            r |= v >> 1;
            return (size - 1) * 8 + r + 1;
        }
    }
}

#endif
