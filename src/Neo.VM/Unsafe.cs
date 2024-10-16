// Copyright (C) 2015-2024 The Neo Project.
//
// Unsafe.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.CompilerServices;

namespace Neo.VM
{
    unsafe internal static class Unsafe
    {
        const long HashMagicNumber = 40343;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotZero(ReadOnlySpan<byte> x)
        {
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
        }

        /// <summary>
        /// Get 64-bit hash code for a byte array
        /// </summary>
        /// <param name="span">Span</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long HashBytes(ReadOnlySpan<byte> span)
        {
            var len = span.Length;
            var hashState = (ulong)len;

            fixed (byte* k = span)
            {
                var pwString = (char*)k;
                var cbBuf = len / 2;

                for (var i = 0; i < cbBuf; i++, pwString++)
                    hashState = HashMagicNumber * hashState + *pwString;

                if ((len & 1) > 0)
                {
                    var pC = (byte*)pwString;
                    hashState = HashMagicNumber * hashState + *pC;
                }
            }

            return (long)Rotr64(HashMagicNumber * hashState, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong Rotr64(ulong x, int n)
        {
            return ((x) >> n) | ((x) << (64 - n));
        }
    }
}
