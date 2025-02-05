// Copyright (C) 2015-2025 The Neo Project.
//
// Unsafe.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    unsafe internal static class Unsafe
    {
        private const long DefaultXxHash3Seed = 40343;

        /// <summary>
        /// All bytes are zero or not in a byte array
        /// </summary>
        /// <param name="x">The byte array</param>
        /// <returns>false if all bytes are zero, true otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotZero(this ReadOnlySpan<byte> x)
        {
#if NET7_0_OR_GREATER
            return x.IndexOfAnyExcept((byte)0) >= 0;
#else
            int len = x.Length;
            if (len == 0) return false;
            fixed (byte* xp = x)
            {
                long* xlp = (long*)xp;
                for (; len >= 8; len -= 8)
                {
                    if (*xlp != 0) return true;
                    xlp++;
                }
                byte* xbp = (byte*)xlp;
                for (; len > 0; len--)
                {
                    if (*xbp != 0) return true;
                    xbp++;
                }
            }
            return false;
#endif
        }

        /// <summary>
        /// Get 64-bit hash code for a byte array
        /// </summary>
        /// <param name="span">Span</param>
        /// <param name="seed">The seed used by the xxhash3 algorithm.</param>
        /// <returns>The computed hash code.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong HashBytes(ReadOnlySpan<byte> span, long seed = DefaultXxHash3Seed)
        {
            return XxHash3.HashToUInt64(span, seed);
        }
    }
}
