// Copyright (C) 2015-2024 The Neo Project.
//
// MillerLoopResult.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Runtime.InteropServices;
using static Neo.Cryptography.BLS12_381.Constants;

namespace Neo.Cryptography.BLS12_381;

[StructLayout(LayoutKind.Explicit, Size = Fp12.Size)]
readonly struct MillerLoopResult
{
    [FieldOffset(0)]
    private readonly Fp12 v;

    public MillerLoopResult(in Fp12 v)
    {
        this.v = v;
    }

    public Gt FinalExponentiation()
    {
        static (Fp2, Fp2) Fp4Square(in Fp2 a, in Fp2 b)
        {
            var t0 = a.Square();
            var t1 = b.Square();
            var t2 = t1.MulByNonresidue();
            var c0 = t2 + t0;
            t2 = a + b;
            t2 = t2.Square();
            t2 -= t0;
            var c1 = t2 - t1;

            return (c0, c1);
        }

        static Fp12 CyclotomicSquare(in Fp12 f)
        {
            var z0 = f.C0.C0;
            var z4 = f.C0.C1;
            var z3 = f.C0.C2;
            var z2 = f.C1.C0;
            var z1 = f.C1.C1;
            var z5 = f.C1.C2;

            var (t0, t1) = Fp4Square(in z0, in z1);

            // For A
            z0 = t0 - z0;
            z0 = z0 + z0 + t0;

            z1 = t1 + z1;
            z1 = z1 + z1 + t1;

            (t0, t1) = Fp4Square(in z2, in z3);
            var (t2, t3) = Fp4Square(in z4, in z5);

            // For C
            z4 = t0 - z4;
            z4 = z4 + z4 + t0;

            z5 = t1 + z5;
            z5 = z5 + z5 + t1;

            // For B
            t0 = t3.MulByNonresidue();
            z2 = t0 + z2;
            z2 = z2 + z2 + t0;

            z3 = t2 - z3;
            z3 = z3 + z3 + t2;

            return new Fp12(new Fp6(in z0, in z4, in z3), new Fp6(in z2, in z1, in z5));
        }

        static Fp12 CycolotomicExp(in Fp12 f)
        {
            var x = BLS_X;
            var tmp = Fp12.One;
            var found_one = false;
            foreach (bool i in Enumerable.Range(0, 64).Select(b => ((x >> b) & 1) == 1).Reverse())
            {
                if (found_one)
                    tmp = CyclotomicSquare(tmp);
                else
                    found_one = i;

                if (i)
                    tmp *= f;
            }

            return tmp.Conjugate();
        }

        var f = v;
        var t0 = f
            .FrobeniusMap()
            .FrobeniusMap()
            .FrobeniusMap()
            .FrobeniusMap()
            .FrobeniusMap()
            .FrobeniusMap();
        var t1 = f.Invert();
        var t2 = t0 * t1;
        t1 = t2;
        t2 = t2.FrobeniusMap().FrobeniusMap();
        t2 *= t1;
        t1 = CyclotomicSquare(t2).Conjugate();
        var t3 = CycolotomicExp(t2);
        var t4 = CyclotomicSquare(t3);
        var t5 = t1 * t3;
        t1 = CycolotomicExp(t5);
        t0 = CycolotomicExp(t1);
        var t6 = CycolotomicExp(t0);
        t6 *= t4;
        t4 = CycolotomicExp(t6);
        t5 = t5.Conjugate();
        t4 *= t5 * t2;
        t5 = t2.Conjugate();
        t1 *= t2;
        t1 = t1.FrobeniusMap().FrobeniusMap().FrobeniusMap();
        t6 *= t5;
        t6 = t6.FrobeniusMap();
        t3 *= t0;
        t3 = t3.FrobeniusMap().FrobeniusMap();
        t3 *= t1;
        t3 *= t6;
        f = t3 * t4;
        return new Gt(f);
    }

    public static MillerLoopResult operator +(in MillerLoopResult a, in MillerLoopResult b)
    {
        return new(a.v * b.v);
    }
}
