// Copyright (C) 2015-2024 The Neo Project.
//
// G2Prepared.Adder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;
using static Neo.Cryptography.BLS12_381.MillerLoopUtility;

namespace Neo.Cryptography.BLS12_381
{
    partial class G2Prepared
    {
        class Adder : IMillerLoopDriver<object?>
        {
            public G2Projective Curve;
            public readonly G2Affine Base;
            public readonly List<(Fp2, Fp2, Fp2)> Coeffs;

            public Adder(in G2Affine q)
            {
                Curve = new G2Projective(in q);
                Base = q;
                Coeffs = new(68);
            }

            object? IMillerLoopDriver<object?>.DoublingStep(in object? f)
            {
                var coeffs = DoublingStep(ref Curve);
                Coeffs.Add(coeffs);
                return null;
            }

            object? IMillerLoopDriver<object?>.AdditionStep(in object? f)
            {
                var coeffs = AdditionStep(ref Curve, in Base);
                Coeffs.Add(coeffs);
                return null;
            }

            #region IMillerLoopDriver<T>

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static object? Square(in object? f) => null;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static object? Conjugate(in object? f) => null;

            public static object? One
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            object? IMillerLoopDriver<object?>.Square(in object? f) => null;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            object? IMillerLoopDriver<object?>.Conjugate(in object? f) => null;

            object? IMillerLoopDriver<object?>.One
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => null;
            }

            #endregion
        }
    }
}
