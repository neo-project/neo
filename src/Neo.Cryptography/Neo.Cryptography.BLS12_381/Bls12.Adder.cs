// Copyright (C) 2015-2024 The Neo Project.
//
// Bls12.Adder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.CompilerServices;
using static Neo.Cryptography.BLS12_381.MillerLoopUtility;

namespace Neo.Cryptography.BLS12_381;

partial class Bls12
{
    class Adder : IMillerLoopDriver<Fp12>
    {
        public G2Projective Curve;
        public readonly G2Affine Base;
        public readonly G1Affine P;

        public Adder(in G1Affine p, in G2Affine q)
        {
            Curve = new(q);
            Base = q;
            P = p;
        }

        Fp12 IMillerLoopDriver<Fp12>.DoublingStep(in Fp12 f)
        {
            var coeffs = DoublingStep(ref Curve);
            return Ell(in f, in coeffs, in P);
        }

        Fp12 IMillerLoopDriver<Fp12>.AdditionStep(in Fp12 f)
        {
            var coeffs = AdditionStep(ref Curve, in Base);
            return Ell(in f, in coeffs, in P);
        }

        #region IMillerLoopDriver<T>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fp12 Square(in Fp12 f) => f.Square();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fp12 Conjugate(in Fp12 f) => f.Conjugate();

        public static Fp12 One
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Fp12.One;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Fp12 IMillerLoopDriver<Fp12>.Square(in Fp12 f) => Adder.Square(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Fp12 IMillerLoopDriver<Fp12>.Conjugate(in Fp12 f) => Adder.Conjugate(f);
        Fp12 IMillerLoopDriver<Fp12>.One
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Adder.One;
        }

        #endregion
    }
}
