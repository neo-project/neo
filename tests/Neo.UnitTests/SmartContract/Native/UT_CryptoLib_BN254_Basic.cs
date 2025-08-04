// Copyright (C) 2015-2025 The Neo Project.
//
// UT_CryptoLib_BN254_Basic.cs file belongs to the neo project and is free
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
    public class UT_CryptoLib_BN254_Basic
    {
        [TestMethod]
        public void TestBn254Serialize_Identity()
        {
            var point = G1Affine.Identity;
            var interop = new InteropInterface(point);

            var result = CryptoLib.Bn254Serialize(interop);
            result.Should().NotBeNull();
            result.Length.Should().Be(32);
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
        public void TestBn254Equal_SameIdentity()
        {
            var a = new InteropInterface(G1Affine.Identity);
            var b = new InteropInterface(G1Affine.Identity);

            CryptoLib.Bn254Equal(a, b).Should().BeTrue();
        }

        [TestMethod]
        public void TestBn254Add_IdentityElements()
        {
            var a = new InteropInterface(G1Affine.Identity);
            var b = new InteropInterface(G1Affine.Identity);

            var result = CryptoLib.Bn254Add(a, b);
            result.Should().NotBeNull();

            var resultPoint = result.GetInterface<G1Projective>();
            resultPoint.Should().NotBeNull();
        }

        [TestMethod]
        public void TestBn254Mul_IdentityByScalar()
        {
            var point = new InteropInterface(G1Affine.Identity);
            var scalar = new byte[32];
            scalar[0] = 5;

            var result = CryptoLib.Bn254Mul(point, scalar, false);
            result.Should().NotBeNull();
        }

        [TestMethod]
        public void TestBn254Pairing_IdentityElements()
        {
            var g1 = new InteropInterface(G1Affine.Identity);
            var g2 = new InteropInterface(G2Affine.Identity);

            var result = CryptoLib.Bn254Pairing(g1, g2);
            result.Should().NotBeNull();
        }

        [TestMethod]
        public void TestBn254PairingCheck_EmptyArrays()
        {
            var g1Array = new InteropInterface[0];
            var g2Array = new InteropInterface[0];

            var result = CryptoLib.Bn254PairingCheck(g1Array, g2Array);
            result.Should().BeTrue();
        }
    }
}
