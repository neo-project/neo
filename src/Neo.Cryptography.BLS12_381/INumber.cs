// Copyright (C) 2015-2024 The Neo Project.
//
// INumber.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;

namespace Neo.Cryptography.BLS12_381;

interface INumber<T> where T : unmanaged, INumber<T>
{
    //static abstract int Size { get; }
    //static abstract ref readonly T Zero { get; }
    //static abstract ref readonly T One { get; }

    //static abstract T operator -(in T x);
    //static abstract T operator +(in T x, in T y);
    //static abstract T operator -(in T x, in T y);
    //static abstract T operator *(in T x, in T y);

    T Negate();
    T Sum(in T value);
    T Subtract(in T value);
    T Multiply(in T value);

    abstract T Square();
}

static class NumberExtensions
{
    private static T PowVartime<T>(T one, T self, ulong[] by) where T : unmanaged, INumber<T>
    {
        // Although this is labeled "vartime", it is only
        // variable time with respect to the exponent.
        var res = one;
        for (var j = by.Length - 1; j >= 0; j--)
        {
            for (var i = 63; i >= 0; i--)
            {
                res = res.Square();
                if (((by[j] >> i) & 1) == 1)
                {
                    res = res.Multiply(self);
                }
            }
        }
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fp PowVartime(this Fp self, ulong[] by) => PowVartime(Fp.One, self, by);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fp2 PowVartime(this Fp2 self, ulong[] by) => PowVartime(Fp2.One, self, by);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fp6 PowVartime(this Fp6 self, ulong[] by) => PowVartime(Fp6.One, self, by);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fp12 PowVartime(this Fp12 self, ulong[] by) => PowVartime(Fp12.One, self, by);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Scalar PowVartime(this Scalar self, ulong[] by) => PowVartime(Scalar.One, self, by);
}
