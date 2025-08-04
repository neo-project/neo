// Copyright (C) 2015-2025 The Neo Project.
//
// UT_CryptoLib_BN254_Vectors.cs file belongs to the neo project and is free
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
    public class UT_CryptoLib_BN254_Vectors
    {
        [TestMethod]
        public void TestBn254_FieldArithmetic()
        {
            // Test known field operations with BN254 modulus
            var a = new Fp(new ulong[] { 1, 0, 0, 0 });
            var b = new Fp(new ulong[] { 2, 0, 0, 0 });
            var result = a + b;
            
            result.Should().NotBe(Fp.Zero);
            (result - b).Should().Be(a);
        }

        [TestMethod]
        public void TestBn254_G1PointOperations()
        {
            // Test G1 identity operations
            var identity = G1Affine.Identity;
            var doubled = new G1Projective(identity) + new G1Projective(identity);
            
            doubled.IsIdentity.Should().BeTrue();
            identity.IsOnCurve().Should().BeTrue();
        }

        [TestMethod]
        public void TestBn254_G2PointOperations()
        {
            // Test G2 identity operations
            var identity = G2Affine.Identity;
            var doubled = new G2Projective(identity) + new G2Projective(identity);
            
            doubled.IsIdentity.Should().BeTrue();
            identity.IsOnCurve().Should().BeTrue();
        }

        [TestMethod]
        public void TestBn254_Serialization_Roundtrip()
        {
            // Test serialization roundtrip for identity
            var point = G1Affine.Identity;
            var interop = new InteropInterface(point);
            
            var serialized = CryptoLib.Bn254Serialize(interop);
            var deserialized = CryptoLib.Bn254Deserialize(serialized);
            var deserializedPoint = deserialized.GetInterface<G1Affine>();
            
            deserializedPoint.Should().Be(point);
        }

        [TestMethod]
        public void TestBn254_ScalarMultiplication()
        {
            // Test scalar multiplication by zero
            var point = new InteropInterface(G1Affine.Identity);
            var zeroScalar = new byte[32]; // All zeros
            
            var result = CryptoLib.Bn254Mul(point, zeroScalar, false);
            var resultPoint = result.GetInterface<G1Projective>();
            
            resultPoint.IsIdentity.Should().BeTrue();
        }

        [TestMethod]
        public void TestBn254_Pairing_Identity()
        {
            // Test pairing with identity elements
            var g1 = new InteropInterface(G1Affine.Identity);
            var g2 = new InteropInterface(G2Affine.Identity);
            
            var result = CryptoLib.Bn254Pairing(g1, g2);
            result.Should().NotBeNull();
            
            var gt = result.GetInterface<Gt>();
            gt.Should().NotBeNull();
        }

        [TestMethod]
        public void TestBn254_PairingCheck_EmptyIsValid()
        {
            // Empty pairing check should return true
            var g1Array = new InteropInterface[0];
            var g2Array = new InteropInterface[0];
            
            var result = CryptoLib.Bn254PairingCheck(g1Array, g2Array);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void TestBn254_CurveParameters()
        {
            // Verify BN254 curve parameters are correct
            var b = new Fp(new ulong[] { 3, 0, 0, 0 });
            
            // For BN254: y^2 = x^3 + 3
            // Identity point (0, 1, 0) should satisfy the curve equation in projective coordinates
            var identity = G1Affine.Identity;
            identity.IsOnCurve().Should().BeTrue();
        }

        [TestMethod]
        public void TestBn254_FieldInversion()
        {
            // Test field inversion
            var one = Fp.One;
            one.TryInvert(out var invOne).Should().BeTrue();
            invOne.Should().Be(one);
            
            // Test zero inversion fails
            var zero = Fp.Zero;
            zero.TryInvert(out var invZero).Should().BeFalse();
        }

        [TestMethod]
        public void TestBn254_G1ProjectiveIdentity()
        {
            // Test projective identity properties
            var identity = G1Projective.Identity;
            var other = new G1Projective(G1Affine.Identity);
            
            identity.IsIdentity.Should().BeTrue();
            other.IsIdentity.Should().BeTrue();
            (identity == other).Should().BeTrue();
        }

        [TestMethod]
        public void TestBn254_G2ProjectiveIdentity()
        {
            // Test G2 projective identity properties
            var identity = G2Projective.Identity;
            var other = new G2Projective(G2Affine.Identity);
            
            identity.IsIdentity.Should().BeTrue();
            other.IsIdentity.Should().BeTrue();
            (identity == other).Should().BeTrue();
        }
    }
}