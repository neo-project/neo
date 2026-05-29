// Copyright (C) 2015-2026 The Neo Project.
//
// UT_BN254Curve.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.BN254Curve;
using System;
using System.Numerics;

namespace Neo.UnitTests.Cryptography
{
    /// <summary>
    /// Field-law and group-law unit tests for the managed BN254 tower
    /// (<see cref="Fp"/>/<see cref="Fp2"/>/<see cref="Fp12"/>, <see cref="EcPoint{T}"/>)
    /// and the optimal-ate pairing. These complement the EIP-196/197 byte-vector tests in
    /// UT_CryptoLib by exercising the hand-ported arithmetic directly: associativity,
    /// inverses, the F_p12 reduction relation, subgroup order, and pairing bilinearity.
    /// </summary>
    [TestClass]
    public class UT_BN254Curve
    {
        private static readonly BigInteger P = Fp.Modulus;
        private static readonly BigInteger R = Bn254Pairing.CurveOrder;

        // py_ecc bn128 generators. G2 coefficients follow the [real, imag] convention.
        private static EcPoint<Fp> G1()
            => EcPoint<Fp>.Create(Fp.FromInteger(1), Fp.FromInteger(2));

        private static EcPoint<Fp2> G2()
            => EcPoint<Fp2>.Create(
                new Fp2(
                    Fp.FromInteger(BigInteger.Parse("10857046999023057135944570762232829481370756359578518086990519993285655852781")),
                    Fp.FromInteger(BigInteger.Parse("11559732032986387107991004021392285783925812861821192530917403151452391805634"))),
                new Fp2(
                    Fp.FromInteger(BigInteger.Parse("8495653923123431417604973247489272438418190587263600148770280649306958101930")),
                    Fp.FromInteger(BigInteger.Parse("4082367875863433681332203403145435568316851327593401208105741076214120093531"))));

        private static bool PointEquals(EcPoint<Fp> a, EcPoint<Fp> b)
        {
            if (a.IsInfinity || b.IsInfinity)
                return a.IsInfinity && b.IsInfinity;
            return a.X.Equals(b.X) && a.Y.Equals(b.Y);
        }

        private static bool PointEquals(EcPoint<Fp2> a, EcPoint<Fp2> b)
        {
            if (a.IsInfinity || b.IsInfinity)
                return a.IsInfinity && b.IsInfinity;
            return a.X.Equals(b.X) && a.Y.Equals(b.Y);
        }

        #region Fp

        [TestMethod]
        public void Fp_Modulus_MatchesBn254Prime()
        {
            Assert.AreEqual(
                BigInteger.Parse("21888242871839275222246405745257275088696311157297823662689037894645226208583"),
                Fp.Modulus);
        }

        [TestMethod]
        public void Fp_FromInteger_NormalizesNegativeAndReducesModulus()
        {
            Assert.IsTrue(Fp.FromInteger(-1).Equals(Fp.FromInteger(P - 1)));
            Assert.IsTrue(Fp.FromInteger(P).IsZero);
            Assert.IsTrue(Fp.FromInteger(P + 5).Equals(Fp.FromInteger(5)));
        }

        [TestMethod]
        public void Fp_AdditiveAndMultiplicativeIdentities()
        {
            var a = Fp.FromInteger(BigInteger.Parse("123456789012345678901234567890123456789"));
            Assert.IsTrue(a.Add(Fp.Zero).Equals(a));
            Assert.IsTrue(a.Multiply(Fp.One).Equals(a));
        }

        [TestMethod]
        public void Fp_NegationAndSubtractionAreConsistent()
        {
            var a = Fp.FromInteger(BigInteger.Parse("999999999999999999999999999999999999"));
            var b = Fp.FromInteger(BigInteger.Parse("123456789012345678901234567890123456789"));
            Assert.IsTrue(a.Add(a.Negate()).IsZero);
            Assert.IsTrue(a.Subtract(b).Equals(a.Add(b.Negate())));
            Assert.IsTrue(a.Subtract(b).Add(b).Equals(a));
        }

        [TestMethod]
        public void Fp_MultiplicationIsCommutative()
        {
            var a = Fp.FromInteger(BigInteger.Parse("999999999999999999999999999999999999"));
            var b = Fp.FromInteger(BigInteger.Parse("123456789012345678901234567890123456789"));
            Assert.IsTrue(a.Multiply(b).Equals(b.Multiply(a)));
        }

        [TestMethod]
        public void Fp_InverseTimesValueIsOne()
        {
            var a = Fp.FromInteger(BigInteger.Parse("123456789012345678901234567890123456789"));
            Assert.IsTrue(a.Multiply(a.Inverse()).Equals(Fp.One));
        }

        [TestMethod]
        public void Fp_InverseOfZeroThrows()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = Fp.Zero.Inverse());
        }

        #endregion

        #region Fp2

        [TestMethod]
        public void Fp2_USquaredIsMinusOne()
        {
            var u = new Fp2(Fp.Zero, Fp.One);
            Assert.IsTrue(u.Multiply(u).Equals(Fp2.One.Negate()));
        }

        [TestMethod]
        public void Fp2_MultiplicationMatchesClosedForm()
        {
            var a0 = Fp.FromInteger(7);
            var a1 = Fp.FromInteger(11);
            var b0 = Fp.FromInteger(13);
            var b1 = Fp.FromInteger(17);
            var product = new Fp2(a0, a1).Multiply(new Fp2(b0, b1));

            // (a0 + a1 u)(b0 + b1 u) = (a0 b0 - a1 b1) + (a0 b1 + a1 b0) u, u^2 = -1.
            Assert.IsTrue(product.C0.Equals(a0.Multiply(b0).Subtract(a1.Multiply(b1))));
            Assert.IsTrue(product.C1.Equals(a0.Multiply(b1).Add(a1.Multiply(b0))));
        }

        [TestMethod]
        public void Fp2_MultiplicationIsAssociativeAndCommutative()
        {
            var a = Fp2.FromIntegers(BigInteger.Parse("12345678901234567890"), 42);
            var b = Fp2.FromIntegers(987654321, BigInteger.Parse("55555555555555555555"));
            var c = Fp2.FromIntegers(1, 2);
            Assert.IsTrue(a.Multiply(b).Multiply(c).Equals(a.Multiply(b.Multiply(c))));
            Assert.IsTrue(a.Multiply(b).Equals(b.Multiply(a)));
        }

        [TestMethod]
        public void Fp2_InverseTimesValueIsOne()
        {
            var a = Fp2.FromIntegers(
                BigInteger.Parse("12345678901234567890"),
                BigInteger.Parse("98765432109876543210"));
            Assert.IsTrue(a.Multiply(a.Inverse()).Equals(Fp2.One));
        }

        #endregion

        #region Fp12

        private static Fp12 SampleFp12(int seed)
        {
            var coeffs = new BigInteger[Fp12.Degree];
            for (int i = 0; i < Fp12.Degree; i++)
                coeffs[i] = BigInteger.Parse("1000000000000000000000") * (i + 1) + seed;
            return Fp12.FromCoefficients(coeffs);
        }

        [TestMethod]
        public void Fp12_Identities()
        {
            var a = SampleFp12(1);
            Assert.IsTrue(a.Multiply(Fp12.One).Equals(a));
            Assert.IsTrue(a.Add(Fp12.Zero).Equals(a));
        }

        [TestMethod]
        public void Fp12_MultiplicationIsAssociativeAndCommutative()
        {
            var a = SampleFp12(1);
            var b = SampleFp12(7);
            var c = SampleFp12(13);
            Assert.IsTrue(a.Multiply(b).Multiply(c).Equals(a.Multiply(b.Multiply(c))));
            Assert.IsTrue(a.Multiply(b).Equals(b.Multiply(a)));
        }

        [TestMethod]
        public void Fp12_InverseTimesValueIsOne()
        {
            var a = SampleFp12(3);
            Assert.IsTrue(a.Multiply(a.Inverse()).Equals(Fp12.One));
        }

        [TestMethod]
        public void Fp12_PowMatchesRepeatedMultiplication()
        {
            var a = SampleFp12(5);
            Assert.IsTrue(a.Pow(0).Equals(Fp12.One));
            Assert.IsTrue(a.Pow(1).Equals(a));
            Assert.IsTrue(a.Pow(2).Equals(a.Multiply(a)));
            Assert.IsTrue(a.Pow(3).Equals(a.Multiply(a).Multiply(a)));
        }

        [TestMethod]
        public void Fp12_ReductionRelation_X12_Equals_18X6_Minus_82()
        {
            var xCoeffs = new BigInteger[Fp12.Degree];
            xCoeffs[1] = BigInteger.One; // the generator X
            var x = Fp12.FromCoefficients(xCoeffs);

            // X^12 ≡ 18 X^6 - 82 (mod the F_p12 defining polynomial).
            var expectedCoeffs = new BigInteger[Fp12.Degree];
            expectedCoeffs[0] = -82;
            expectedCoeffs[6] = 18;
            var expected = Fp12.FromCoefficients(expectedCoeffs);

            Assert.IsTrue(x.Pow(12).Equals(expected));
        }

        #endregion

        #region G1 (EcPoint over Fp)

        [TestMethod]
        public void G1_GeneratorIsOnCurve()
        {
            Assert.IsTrue(G1().IsOnCurve(Bn254Pairing.B1));
        }

        [TestMethod]
        public void G1_InfinityIsAdditiveIdentity()
        {
            var g = G1();
            Assert.IsTrue(PointEquals(g.Add(EcPoint<Fp>.Infinity), g));
            Assert.IsTrue(PointEquals(EcPoint<Fp>.Infinity.Add(g), g));
        }

        [TestMethod]
        public void G1_PointPlusNegationIsInfinity()
        {
            var g = G1();
            Assert.IsTrue(g.Add(g.Negate()).IsInfinity);
        }

        [TestMethod]
        public void G1_DoubleEqualsSelfAddition()
        {
            var g = G1();
            Assert.IsTrue(PointEquals(g.Double(), g.Add(g)));
            Assert.IsTrue(g.Double().IsOnCurve(Bn254Pairing.B1));
        }

        [TestMethod]
        public void G1_ScalarMultiplicationBaseCases()
        {
            var g = G1();
            Assert.IsTrue(g.Multiply(0).IsInfinity);
            Assert.IsTrue(PointEquals(g.Multiply(1), g));
            Assert.IsTrue(PointEquals(g.Multiply(2), g.Double()));
        }

        [TestMethod]
        public void G1_ScalarMultiplicationIsDistributive()
        {
            var g = G1();
            Assert.IsTrue(PointEquals(g.Multiply(12), g.Multiply(5).Add(g.Multiply(7))));
        }

        [TestMethod]
        public void G1_HasPrimeOrderR()
        {
            Assert.IsTrue(G1().Multiply(R).IsInfinity);
        }

        [TestMethod]
        public void G1_NegativeScalarThrows()
        {
            var g = G1();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = g.Multiply(BigInteger.MinusOne));
        }

        #endregion

        #region G2 (EcPoint over Fp2)

        [TestMethod]
        public void G2_GeneratorIsOnTwist()
        {
            Assert.IsTrue(G2().IsOnCurve(Bn254Pairing.B2));
        }

        [TestMethod]
        public void G2_DoubleEqualsSelfAddition()
        {
            var g = G2();
            Assert.IsTrue(PointEquals(g.Double(), g.Add(g)));
            Assert.IsTrue(g.Double().IsOnCurve(Bn254Pairing.B2));
        }

        [TestMethod]
        public void G2_HasPrimeOrderR()
        {
            Assert.IsTrue(G2().Multiply(R).IsInfinity);
        }

        #endregion

        #region Pairing

        [TestMethod]
        public void Pairing_EmptySetIsIdentity()
        {
            Assert.IsTrue(Bn254Pairing.PairingCheck(System.Array.Empty<(EcPoint<Fp>, EcPoint<Fp2>)>()));
        }

        [TestMethod]
        public void Pairing_InfinityPairsAreSkipped()
        {
            Assert.IsTrue(Bn254Pairing.PairingCheck(new[] { (G1(), EcPoint<Fp2>.Infinity) }));
            Assert.IsTrue(Bn254Pairing.PairingCheck(new[] { (EcPoint<Fp>.Infinity, G2()) }));
        }

        [TestMethod]
        public void Pairing_GeneratorsAreNonDegenerate()
        {
            // e(G1, G2) != 1, so a single generator pair must not be the identity.
            Assert.IsFalse(Bn254Pairing.PairingCheck(new[] { (G1(), G2()) }));
        }

        [TestMethod]
        public void Pairing_ProductWithInverseIsIdentity()
        {
            // e(G1, G2) * e(-G1, G2) = e(G1, G2) * e(G1, G2)^-1 = 1.
            Assert.IsTrue(Bn254Pairing.PairingCheck(new[]
            {
                (G1(), G2()),
                (G1().Negate(), G2()),
            }));
        }

        [TestMethod]
        public void Pairing_IsBilinearAcrossG1AndG2()
        {
            // e(a*G1, G2) * e(G1, (r-a)*G2) = e(G1,G2)^a * e(G1,G2)^(-a) = 1.
            var a = new BigInteger(5);
            Assert.IsTrue(Bn254Pairing.PairingCheck(new[]
            {
                (G1().Multiply(a), G2()),
                (G1(), G2().Multiply(R - a)),
            }));
        }

        [TestMethod]
        public void Pairing_BilinearityScalarsCommute()
        {
            // e(a*G1, b*G2) = e(ab*G1, G2): pairing the first against the negated second is 1.
            var a = new BigInteger(5);
            var b = new BigInteger(7);
            var negAb = (R - (a * b % R)) % R;
            Assert.IsTrue(Bn254Pairing.PairingCheck(new[]
            {
                (G1().Multiply(a), G2().Multiply(b)),
                (G1().Multiply(negAb), G2()),
            }));
        }

        [TestMethod]
        public void Pairing_IsAdditiveInSecondArgument()
        {
            // e(P, Q+S) = e(P, Q) * e(P, S); the combined check below must be the identity.
            var q = G2();
            var s = G2().Multiply(3);
            Assert.IsTrue(Bn254Pairing.PairingCheck(new[]
            {
                (G1(), q.Add(s)),
                (G1().Negate(), q),
                (G1().Negate(), s),
            }));
        }

        #endregion
    }
}
