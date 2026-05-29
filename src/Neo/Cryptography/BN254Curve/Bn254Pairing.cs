// Copyright (C) 2015-2026 The Neo Project.
//
// Bn254Pairing.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Numerics;

namespace Neo.Cryptography.BN254Curve
{
    /// <summary>
    /// Optimal-ate pairing over BN254, a faithful managed port of py_ecc's bn128 pairing.
    /// Exposes the curve constants, the twist that embeds an F_p2 point of G2 into F_p12,
    /// the Miller loop, the final exponentiation, and the EIP-197 multi-pairing product check.
    /// </summary>
    internal static class Bn254Pairing
    {
        /// <summary>The BN254 base field modulus p.</summary>
        public static readonly BigInteger FieldModulus = Fp.Modulus;

        /// <summary>The BN254 prime group order r.</summary>
        public static readonly BigInteger CurveOrder = BigInteger.Parse(
            "21888242871839275222246405745257275088548364400416034343698204186575808495617");

        /// <summary>Curve constant b = 3 for G1 (y^2 = x^3 + 3 over F_p).</summary>
        public static readonly Fp B1 = Fp.FromInteger(3);

        /// <summary>Twist curve constant b2 = (3 + 0u) / (9 + u) over F_p2.</summary>
        public static readonly Fp2 B2 = Fp2.FromIntegers(3, 0).Multiply(Fp2.FromIntegers(9, 1).Inverse());

        // ate_loop_count from py_ecc; its top set bit is handled by initializing R = Q,
        // so the Miller loop only scans bits log_ate_loop_count..0.
        private static readonly BigInteger s_ateLoopCount = BigInteger.Parse("29793968203157093288");
        private const int LogAteLoopCount = 63;

        // Final exponentiation exponent (p^12 - 1) / r.
        private static readonly BigInteger s_finalExponent =
            (BigInteger.Pow(Fp.Modulus, 12) - BigInteger.One) / CurveOrder;

        // w^2 and w^3 in the flat F_p12 basis, where w is the degree-1 generator X.
        private static readonly Fp12 s_wSquared = MakeMonomial(2);
        private static readonly Fp12 s_wCubed = MakeMonomial(3);

        private static readonly BigInteger s_nine = new(9);

        /// <summary>
        /// EIP-197 multi-pairing check: returns <see langword="true"/> iff the product of
        /// e(g1_i, g2_i) over all pairs equals one in the target group. Pairs that contain a
        /// point at infinity contribute the identity and are skipped; an empty effective set
        /// also returns <see langword="true"/>.
        /// </summary>
        public static bool PairingCheck(IReadOnlyList<(EcPoint<Fp> G1, EcPoint<Fp2> G2)> pairs)
        {
            var accumulator = Fp12.One;
            var hasEffectivePair = false;

            foreach (var (g1, g2) in pairs)
            {
                if (g1.IsInfinity || g2.IsInfinity)
                    continue;

                var miller = MillerLoop(Twist(g2), CastG1ToFp12(g1), finalExponentiate: false);
                accumulator = hasEffectivePair ? accumulator.Multiply(miller) : miller;
                hasEffectivePair = true;
            }

            if (!hasEffectivePair)
                return true;

            return FinalExponentiate(accumulator).Equals(Fp12.One);
        }

        /// <summary>
        /// Computes the optimal-ate pairing e(g1, g2) and returns the fully reduced target-group
        /// element (Miller loop followed by final exponentiation). A point at infinity in either
        /// argument yields the target-group identity <see cref="Fp12.One"/>, because the twist and
        /// the F_p12 embedding map infinity to infinity and the Miller loop returns one. The result
        /// lives in the order-r subgroup of F_p12*, so its group operation is field multiplication;
        /// this is the building block for Groth16-style multi-pairing checks composed by the caller.
        /// </summary>
        public static Fp12 Pairing(EcPoint<Fp> g1, EcPoint<Fp2> g2)
            => MillerLoop(Twist(g2), CastG1ToFp12(g1), finalExponentiate: true);

        /// <summary>Embeds a G1 point over F_p into F_p12 (constant-term embedding).</summary>
        public static EcPoint<Fp12> CastG1ToFp12(EcPoint<Fp> point)
        {
            if (point.IsInfinity)
                return EcPoint<Fp12>.Infinity;

            var xCoeffs = new BigInteger[Fp12.Degree];
            var yCoeffs = new BigInteger[Fp12.Degree];
            xCoeffs[0] = point.X.Value;
            yCoeffs[0] = point.Y.Value;
            return EcPoint<Fp12>.Create(Fp12.FromCoefficients(xCoeffs), Fp12.FromCoefficients(yCoeffs));
        }

        /// <summary>
        /// Twists a point of the F_p2 sextic twist into the F_p12 curve, matching py_ecc:
        /// apply the field isomorphism x -&gt; x - 9u, embed into the w^6 = x subfield, then
        /// divide the x coordinate by w^2 and the y coordinate by w^3.
        /// </summary>
        public static EcPoint<Fp12> Twist(EcPoint<Fp2> point)
        {
            if (point.IsInfinity)
                return EcPoint<Fp12>.Infinity;

            var x = point.X;
            var y = point.Y;

            var xc0 = x.C0.Subtract(x.C1.Multiply(Fp.FromInteger(s_nine)));
            var xc1 = x.C1;
            var yc0 = y.C0.Subtract(y.C1.Multiply(Fp.FromInteger(s_nine)));
            var yc1 = y.C1;

            var nxCoeffs = new BigInteger[Fp12.Degree];
            nxCoeffs[0] = xc0.Value;
            nxCoeffs[6] = xc1.Value;

            var nyCoeffs = new BigInteger[Fp12.Degree];
            nyCoeffs[0] = yc0.Value;
            nyCoeffs[6] = yc1.Value;

            var nx = Fp12.FromCoefficients(nxCoeffs).Multiply(s_wSquared);
            var ny = Fp12.FromCoefficients(nyCoeffs).Multiply(s_wCubed);
            return EcPoint<Fp12>.Create(nx, ny);
        }

        /// <summary>Final exponentiation f^((p^12 - 1) / r) mapping a Miller value into the target group.</summary>
        public static Fp12 FinalExponentiate(Fp12 value) => value.Pow(s_finalExponent);

        /// <summary>
        /// Miller loop over the ate loop count. When <paramref name="finalExponentiate"/> is
        /// false the raw Miller value is returned, which is required to accumulate a multi-pairing
        /// product before a single final exponentiation.
        /// </summary>
        public static Fp12 MillerLoop(EcPoint<Fp12> q, EcPoint<Fp12> p, bool finalExponentiate)
        {
            if (q.IsInfinity || p.IsInfinity)
                return Fp12.One;

            var r = q;
            var f = Fp12.One;

            for (int i = LogAteLoopCount; i >= 0; i--)
            {
                f = f.Multiply(f).Multiply(LineFunc(r, r, p));
                r = r.Double();
                if (!((s_ateLoopCount >> i) & BigInteger.One).IsZero)
                {
                    f = f.Multiply(LineFunc(r, q, p));
                    r = r.Add(q);
                }
            }

            var q1 = EcPoint<Fp12>.Create(q.X.Pow(FieldModulus), q.Y.Pow(FieldModulus));
            var nq2 = EcPoint<Fp12>.Create(q1.X.Pow(FieldModulus), q1.Y.Pow(FieldModulus).Negate());

            f = f.Multiply(LineFunc(r, q1, p));
            r = r.Add(q1);
            f = f.Multiply(LineFunc(r, nq2, p));

            return finalExponentiate ? FinalExponentiate(f) : f;
        }

        // Evaluates the line through P1 and P2 (tangent when equal) at the point T, returning
        // the line value as a field element. All three points must be finite.
        private static Fp12 LineFunc(EcPoint<Fp12> p1, EcPoint<Fp12> p2, EcPoint<Fp12> t)
        {
            var x1 = p1.X;
            var y1 = p1.Y;
            var x2 = p2.X;
            var y2 = p2.Y;
            var xt = t.X;
            var yt = t.Y;

            if (!x1.Equals(x2))
            {
                var slope = y2.Subtract(y1).Multiply(x2.Subtract(x1).Inverse());
                return slope.Multiply(xt.Subtract(x1)).Subtract(yt.Subtract(y1));
            }

            if (y1.Equals(y2))
            {
                var x1Squared = x1.Multiply(x1);
                var numerator = x1Squared.Add(x1Squared).Add(x1Squared);
                var slope = numerator.Multiply(y1.Add(y1).Inverse());
                return slope.Multiply(xt.Subtract(x1)).Subtract(yt.Subtract(y1));
            }

            return xt.Subtract(x1);
        }

        private static Fp12 MakeMonomial(int index)
        {
            var coeffs = new BigInteger[Fp12.Degree];
            coeffs[index] = BigInteger.One;
            return Fp12.FromCoefficients(coeffs);
        }
    }
}
