// Copyright (C) 2015-2024 The Neo Project.
//
// MillerLoopUtility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using static Neo.Cryptography.BLS12_381.Constants;

namespace Neo.Cryptography.BLS12_381
{
    static class MillerLoopUtility
    {
        public static T MillerLoop<T, D>(D driver) where D : IMillerLoopDriver<T>
        {
            var f = driver.One;

            var found_one = false;
            foreach (var i in Enumerable.Range(0, 64).Reverse().Select(b => ((BLS_X >> 1 >> b) & 1) == 1))
            {
                if (!found_one)
                {
                    found_one = i;
                    continue;
                }

                f = driver.DoublingStep(f);

                if (i)
                    f = driver.AdditionStep(f);

                f = driver.Square(f);
            }

            f = driver.DoublingStep(f);

            if (BLS_X_IS_NEGATIVE)
                f = driver.Conjugate(f);

            return f;
        }

        public static Fp12 Ell(in Fp12 f, in (Fp2 X, Fp2 Y, Fp2 Z) coeffs, in G1Affine p)
        {
            var c0 = new Fp2(coeffs.X.C0 * p.Y, coeffs.X.C1 * p.Y);
            var c1 = new Fp2(coeffs.Y.C0 * p.X, coeffs.Y.C1 * p.X);
            return f.MulBy_014(in coeffs.Z, in c1, in c0);
        }

        public static (Fp2, Fp2, Fp2) DoublingStep(ref G2Projective r)
        {
            // Adaptation of Algorithm 26, https://eprint.iacr.org/2010/354.pdf
            var tmp0 = r.X.Square();
            var tmp1 = r.Y.Square();
            var tmp2 = tmp1.Square();
            var tmp3 = (tmp1 + r.X).Square() - tmp0 - tmp2;
            tmp3 += tmp3;
            var tmp4 = tmp0 + tmp0 + tmp0;
            var tmp6 = r.X + tmp4;
            var tmp5 = tmp4.Square();
            var zsquared = r.Z.Square();
            var x = tmp5 - tmp3 - tmp3;
            var z = (r.Z + r.Y).Square() - tmp1 - zsquared;
            var y = (tmp3 - x) * tmp4;
            tmp2 += tmp2;
            tmp2 += tmp2;
            tmp2 += tmp2;
            y -= tmp2;
            r = new(in x, in y, in z);
            tmp3 = tmp4 * zsquared;
            tmp3 += tmp3;
            tmp3 = -tmp3;
            tmp6 = tmp6.Square() - tmp0 - tmp5;
            tmp1 += tmp1;
            tmp1 += tmp1;
            tmp6 -= tmp1;
            tmp0 = r.Z * zsquared;
            tmp0 += tmp0;

            return (tmp0, tmp3, tmp6);
        }

        public static (Fp2, Fp2, Fp2) AdditionStep(ref G2Projective r, in G2Affine q)
        {
            // Adaptation of Algorithm 27, https://eprint.iacr.org/2010/354.pdf
            var zsquared = r.Z.Square();
            var ysquared = q.Y.Square();
            var t0 = zsquared * q.X;
            var t1 = ((q.Y + r.Z).Square() - ysquared - zsquared) * zsquared;
            var t2 = t0 - r.X;
            var t3 = t2.Square();
            var t4 = t3 + t3;
            t4 += t4;
            var t5 = t4 * t2;
            var t6 = t1 - r.Y - r.Y;
            var t9 = t6 * q.X;
            var t7 = t4 * r.X;
            var x = t6.Square() - t5 - t7 - t7;
            var z = (r.Z + t2).Square() - zsquared - t3;
            var t10 = q.Y + z;
            var t8 = (t7 - x) * t6;
            t0 = r.Y * t5;
            t0 += t0;
            var y = t8 - t0;
            r = new(in x, in y, in z);
            t10 = t10.Square() - ysquared;
            var ztsquared = r.Z.Square();
            t10 -= ztsquared;
            t9 = t9 + t9 - t10;
            t10 = r.Z + r.Z;
            t6 = -t6;
            t1 = t6 + t6;

            return (t10, t1, t9);
        }
    }
}
