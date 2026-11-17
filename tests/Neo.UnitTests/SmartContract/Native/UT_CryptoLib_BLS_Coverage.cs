// Copyright (C) 2015-2026 The Neo Project.
//
// UT_CryptoLib_BLS_Coverage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.BLS12_381;
using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Text;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_CryptoLib_BLS_Coverage
    {
        private ApplicationEngine _engine;

        [TestInitialize]
        public void Initialize()
        {
            _engine = ApplicationEngine.Create(TriggerType.Application, null,
                TestBlockchain.GetTestSnapshotCache(), settings: TestProtocolSettings.Default);
        }

        [TestCleanup]
        public void Cleanup() => _engine.Dispose();

        private static readonly byte[] G1 = (
            "97f1d3a73197d7942695638c4fa9ac0fc3688c4f9774b905a14e3a3f171bac586c55e83ff97a1aeffb3af00adb22c6bb"
        ).HexToBytes();

        private static readonly byte[] G2 = (
            "93e02b6052719f607dacd3a088274f65596bd0d09920b61ab5da61bbdc7f5049334cf11213945d57e5ac7d055d042b7e" +
            "024aa2b2f08f0a91260805272dc51051c6e47ad4fa403b02b4510b647ae3d1770bac0326a805bbefd48056c8c121bdb8"
        ).HexToBytes();

        [TestMethod]
        public void Bls12381Deserialize_InvalidLength_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => CryptoLib.Bls12381Deserialize(_engine, [1, 2, 3]));
        }

        [TestMethod]
        public void Bls12381Serialize_UnknownType_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
                CryptoLib.Bls12381Serialize(new InteropInterface("not-a-bls-point")));
        }

        [TestMethod]
        public void Bls12381Equal_TypeMismatch_Throws()
        {
            var g1 = CryptoLib.Bls12381Deserialize(_engine, G1);
            var g2 = CryptoLib.Bls12381Deserialize(_engine, G2);
            Assert.ThrowsExactly<ArgumentException>(() => CryptoLib.Bls12381Equal(g1, g2));
        }

        [TestMethod]
        public void Bls12381Equal_SameG1_True()
        {
            var a = CryptoLib.Bls12381Deserialize(_engine, G1);
            var b = CryptoLib.Bls12381Deserialize(_engine, G1);
            Assert.IsTrue(CryptoLib.Bls12381Equal(a, b));
        }

        [TestMethod]
        public void Bls12381Serialize_Deserialize_G1_RoundTrip()
        {
            var original = CryptoLib.Bls12381Deserialize(_engine, G1);
            var bytes = CryptoLib.Bls12381Serialize(original);
            Assert.AreEqual(48, bytes.Length);
            var restored = CryptoLib.Bls12381Deserialize(_engine, bytes);
            Assert.IsTrue(CryptoLib.Bls12381Equal(original, restored));
        }

        [TestMethod]
        public void Bls12381Serialize_Deserialize_G2Identity_RoundTrip()
        {
            _engine.SnapshotCache.Add(StorageKey.Create(NativeContract.Policy.Id, 25, Encoding.UTF8.GetBytes("Iara")),
                new StorageItem(0u));
            byte[] identity = new byte[96];
            identity[0] = 0xc0;

            var original = new InteropInterface(G2Affine.Identity);
            byte[] encoded = CryptoLib.Bls12381Serialize(original);
            Assert.AreSequenceEqual(identity, encoded);

            var restored = CryptoLib.Bls12381Deserialize(_engine, encoded);
            Assert.IsTrue(CryptoLib.Bls12381Equal(original, restored));
        }

        [TestMethod]
        public void Bls12381Deserialize_NoncanonicalG2Identity_Throws()
        {
            byte[] identity = new byte[96];
            identity[0] = 0xc0;

            byte[] nonzeroX = (byte[])identity.Clone();
            nonzeroX[^1] = 1;
            Assert.Throws<Exception>(() => CryptoLib.Bls12381Deserialize(_engine, nonzeroX));

            byte[] sortFlag = (byte[])identity.Clone();
            sortFlag[0] = 0xe0;
            Assert.Throws<Exception>(() => CryptoLib.Bls12381Deserialize(_engine, sortFlag));
        }

        [TestMethod]
        public void Bls12381Deserialize_Gt_ValidatesSubgroupFromJackalope()
        {
            byte[] zero = new byte[576];
            byte[] orderTwo = new Gt(-Fp12.One).ToArray();
            byte[] outsideSubgroup = new Gt(Fp12.One + Fp12.One).ToArray();

            // Historical blocks retain the old decoding behavior.
            CryptoLib.Bls12381Deserialize(_engine, zero);
            CryptoLib.Bls12381Deserialize(_engine, orderTwo);
            CryptoLib.Bls12381Deserialize(_engine, outsideSubgroup);

            _engine.SnapshotCache.Add(StorageKey.Create(NativeContract.Policy.Id, 25, Encoding.UTF8.GetBytes("Iara")),
                new StorageItem(0u));
            _engine.SnapshotCache.Add(StorageKey.Create(NativeContract.Policy.Id, 25, Encoding.UTF8.GetBytes("Jackalope")),
                new StorageItem(0u));

            Assert.ThrowsExactly<FormatException>(() => CryptoLib.Bls12381Deserialize(_engine, zero));
            Assert.ThrowsExactly<FormatException>(() => CryptoLib.Bls12381Deserialize(_engine, orderTwo));
            Assert.ThrowsExactly<FormatException>(() => CryptoLib.Bls12381Deserialize(_engine, outsideSubgroup));

            var identity = CryptoLib.Bls12381Deserialize(_engine, Gt.Identity.ToArray());
            Assert.IsTrue(identity.GetInterface<Gt>().IsIdentity);

            G1Affine g1 = G1Affine.FromCompressed(G1);
            G2Affine g2 = G2Affine.FromCompressed(G2);
            byte[] pairing = Bls12.Pairing(in g1, in g2).ToArray();
            Assert.AreSequenceEqual(pairing, CryptoLib.Bls12381Deserialize(_engine, pairing).GetInterface<Gt>().ToArray());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Bls12381Deserialize_Gt_NativeCallRespectsJackalope(bool jackalope)
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            if (jackalope)
            {
                snapshotCache.Add(StorageKey.Create(NativeContract.Policy.Id, 25, Encoding.UTF8.GetBytes("Iara")),
                    new StorageItem(0u));
                snapshotCache.Add(StorageKey.Create(NativeContract.Policy.Id, 25, Encoding.UTF8.GetBytes("Jackalope")),
                    new StorageItem(0u));
            }

            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.CryptoLib.Hash, "bls12381Deserialize", new byte[576]);
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache,
                settings: TestProtocolSettings.Default);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(jackalope ? VMState.FAULT : VMState.HALT, engine.Execute());
        }

        [TestMethod]
        public void Bls12381Add_G1_ReturnsExpectedSum()
        {
            var a = CryptoLib.Bls12381Deserialize(_engine, G1);
            var b = CryptoLib.Bls12381Deserialize(_engine, G1);
            var sum = CryptoLib.Bls12381Add(a, b);

            var affine = a.GetInterface<G1Affine>();
            var expected = new G1Affine(new G1Projective(affine) + affine).ToCompressed();
            var actual = CryptoLib.Bls12381Serialize(sum);

            Assert.AreSequenceEqual(expected, actual);
        }

        [TestMethod]
        public void Bls12381Add_TypeMismatch_Throws()
        {
            var g1 = CryptoLib.Bls12381Deserialize(_engine, G1);
            var g2 = CryptoLib.Bls12381Deserialize(_engine, G2);
            Assert.ThrowsExactly<ArgumentException>(() => CryptoLib.Bls12381Add(g1, g2));
        }

        [TestMethod]
        public void Bls12381Serialize_G1Projective_Works()
        {
            var affine = CryptoLib.Bls12381Deserialize(_engine, G1).GetInterface<G1Affine>();
            var projective = new InteropInterface(new G1Projective(affine));
            var bytes = CryptoLib.Bls12381Serialize(projective);
            Assert.AreEqual(48, bytes.Length);
        }
    }
}
