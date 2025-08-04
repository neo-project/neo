// Copyright (C) 2015-2025 The Neo Project.
//
// UT_CryptoLib.BN254.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.BN254;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_CryptoLib_BN254
    {

        [TestMethod]
        public void TestBn254Serialize_G1()
        {
            var point = G1Affine.Identity;
            var interop = new InteropInterface(point);

            var result = CryptoLib.Bn254Serialize(interop);
            result.Should().NotBeNull();
            result.Length.Should().Be(32);
        }

        [TestMethod]
        public void TestBn254Serialize_G2()
        {
            var point = G2Affine.Identity;
            var interop = new InteropInterface(point);

            var result = CryptoLib.Bn254Serialize(interop);
            result.Should().NotBeNull();
            result.Length.Should().Be(64);
        }

        [TestMethod]
        public void TestBn254Serialize_Gt()
        {
            var point = Gt.Identity;
            var interop = new InteropInterface(point);

            var result = CryptoLib.Bn254Serialize(interop);
            result.Should().NotBeNull();
            result.Length.Should().Be(384);
        }

        [TestMethod]
        public void TestBn254Serialize_InvalidType()
        {
            var invalid = new InteropInterface("invalid");

            Action act = () => CryptoLib.Bn254Serialize(invalid);
            act.Should().Throw<ArgumentException>()
                .WithMessage("BN254 type mismatch");
        }

        [TestMethod]
        public void TestBn254Deserialize_InvalidLength()
        {
            var invalidData = new byte[50]; // Invalid length

            Action act = () => CryptoLib.Bn254Deserialize(invalidData);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Invalid BN254 point length");
        }

        [TestMethod]
        public void TestBn254Equal_G1()
        {
            var a = new InteropInterface(G1Affine.Identity);
            var b = new InteropInterface(G1Affine.Identity);
            var c = new InteropInterface(G1Affine.Identity);

            CryptoLib.Bn254Equal(a, b).Should().BeTrue();
            CryptoLib.Bn254Equal(a, c).Should().BeTrue();
        }

        [TestMethod]
        public void TestBn254Equal_TypeMismatch()
        {
            var g1 = new InteropInterface(G1Affine.Identity);
            var g2 = new InteropInterface(G2Affine.Identity);

            Action act = () => CryptoLib.Bn254Equal(g1, g2);
            act.Should().Throw<ArgumentException>()
                .WithMessage("BN254 type mismatch");
        }

        [TestMethod]
        public void TestBn254Add_G1()
        {
            var a = new InteropInterface(G1Affine.Identity);
            var b = new InteropInterface(G1Affine.Identity);

            var result = CryptoLib.Bn254Add(a, b);
            result.Should().NotBeNull();

            var resultPoint = result.GetInterface<G1Projective>();
            resultPoint.Should().NotBeNull();
        }

        [TestMethod]
        public void TestBn254Add_Mixed()
        {
            var affine = new InteropInterface(G1Affine.Identity);
            var projective = new InteropInterface(G1Projective.Identity);

            var result1 = CryptoLib.Bn254Add(affine, projective);
            var result2 = CryptoLib.Bn254Add(projective, affine);

            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
        }

        [TestMethod]
        public void TestBn254Mul_G1()
        {
            var point = new InteropInterface(G1Affine.Identity);
            var scalar = new byte[32];
            scalar[0] = 2; // Scalar value 2

            var result = CryptoLib.Bn254Mul(point, scalar, false);
            result.Should().NotBeNull();

            var resultPoint = result.GetInterface<G1Projective>();
            resultPoint.Should().NotBeNull();
        }

        [TestMethod]
        public void TestBn254Mul_Negation()
        {
            var point = new InteropInterface(G1Affine.Identity);
            var scalar = new byte[32];
            scalar[0] = 1; // Scalar value 1

            var result = CryptoLib.Bn254Mul(point, scalar, true); // Negative
            result.Should().NotBeNull();
        }

        [TestMethod]
        public void TestBn254Pairing()
        {
            var g1 = new InteropInterface(G1Affine.Identity);
            var g2 = new InteropInterface(G2Affine.Identity);

            var result = CryptoLib.Bn254Pairing(g1, g2);
            result.Should().NotBeNull();

            var gt = result.GetInterface<Gt>();
            gt.Should().NotBeNull();
        }

        [TestMethod]
        public void TestBn254Pairing_InvalidG1Type()
        {
            var invalid = new InteropInterface("invalid");
            var g2 = new InteropInterface(G2Affine.Identity);

            Action act = () => CryptoLib.Bn254Pairing(invalid, g2);
            act.Should().Throw<ArgumentException>()
                .WithMessage("BN254 type mismatch");
        }

        [TestMethod]
        public void TestBn254PairingCheck_Empty()
        {
            var g1Array = new InteropInterface[0];
            var g2Array = new InteropInterface[0];

            var result = CryptoLib.Bn254PairingCheck(g1Array, g2Array);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void TestBn254PairingCheck_SinglePair()
        {
            var g1Array = new[] { new InteropInterface(G1Affine.Identity) };
            var g2Array = new[] { new InteropInterface(G2Affine.Identity) };

            var result = CryptoLib.Bn254PairingCheck(g1Array, g2Array);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void TestBn254PairingCheck_MismatchedLengths()
        {
            var g1Array = new[] { new InteropInterface(G1Affine.Identity) };
            var g2Array = new InteropInterface[0];

            Action act = () => CryptoLib.Bn254PairingCheck(g1Array, g2Array);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Input arrays must have the same length");
        }

        [TestMethod]
        public void TestBn254PairingCheck_InvalidG1Type()
        {
            var g1Array = new[] { new InteropInterface("invalid") };
            var g2Array = new[] { new InteropInterface(G2Affine.Generator) };

            Action act = () => CryptoLib.Bn254PairingCheck(g1Array, g2Array);
            act.Should().Throw<ArgumentException>()
                .WithMessage("BN254 G1 type mismatch");
        }

        [TestMethod]
        public void TestBn254PairingCheck_InvalidG2Type()
        {
            var g1Array = new[] { new InteropInterface(G1Affine.Identity) };
            var g2Array = new[] { new InteropInterface("invalid") };

            Action act = () => CryptoLib.Bn254PairingCheck(g1Array, g2Array);
            act.Should().Throw<ArgumentException>()
                .WithMessage("BN254 G2 type mismatch");
        }
    }
}
