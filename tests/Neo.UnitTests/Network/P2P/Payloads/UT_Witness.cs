// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Witness.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Witness
    {

        [TestMethod]
        public void InvocationScript_Get()
        {
            var uut = Witness.Empty;
            Assert.IsTrue(uut.InvocationScript.IsEmpty);
        }

        private static Witness PrepareDummyWitness(int pubKeys, int m)
        {
            var address = new WalletAccount[pubKeys];
            var wallets = new NEP6Wallet[pubKeys];
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();

            for (int x = 0; x < pubKeys; x++)
            {
                wallets[x] = TestUtils.GenerateTestWallet("123");
                address[x] = wallets[x].CreateAccount();
            }

            // Generate multisignature

            var multiSignContract = Contract.CreateMultiSigContract(m, address.Select(a => a.GetKey().PublicKey).ToArray());

            for (int x = 0; x < pubKeys; x++)
            {
                wallets[x].CreateAccount(multiSignContract, address[x].GetKey());
            }

            // Sign

            var data = new ContractParametersContext(snapshotCache, new Transaction()
            {
                Attributes = [],
                Signers = [new() { Account = multiSignContract.ScriptHash, Scopes = WitnessScope.CalledByEntry }],
                NetworkFee = 0,
                Nonce = 0,
                Script = ReadOnlyMemory<byte>.Empty,
                SystemFee = 0,
                ValidUntilBlock = 0,
                Version = 0,
                Witnesses = []
            }, TestProtocolSettings.Default.Network);

            for (int x = 0; x < m; x++)
            {
                Assert.IsTrue(wallets[x].Sign(data));
            }

            Assert.IsTrue(data.Completed);
            return data.GetWitnesses()[0];
        }

        [TestMethod]
        public void MaxSize_OK()
        {
            var witness = PrepareDummyWitness(10, 10);

            // Check max size

            Assert.AreEqual(1023, witness.Size);
            Assert.AreEqual(663, witness.InvocationScript.GetVarSize());
            Assert.AreEqual(360, witness.VerificationScript.GetVarSize());

            var copy = witness.ToArray().AsSerializable<Witness>();

            Assert.IsTrue(witness.InvocationScript.Span.SequenceEqual(copy.InvocationScript.Span));
            Assert.IsTrue(witness.VerificationScript.Span.SequenceEqual(copy.VerificationScript.Span));
        }

        [TestMethod]
        public void MaxSize_Error()
        {
            var witness = new Witness
            {
                InvocationScript = new byte[1025],
                VerificationScript = new byte[10]
            };

            // Check max size

            Assert.ThrowsExactly<FormatException>(() => _ = witness.ToArray().AsSerializable<Witness>());

            // Check max size

            witness.InvocationScript = new byte[10];
            witness.VerificationScript = new byte[1025];
            Assert.ThrowsExactly<FormatException>(() => _ = witness.ToArray().AsSerializable<Witness>());
        }

        [TestMethod]
        public void InvocationScript_Set()
        {
            var uut = new Witness() { InvocationScript = new byte[] { 0, 32, 32, 20, 32, 32 } };
            Assert.AreEqual(6, uut.InvocationScript.Length);
            Assert.AreEqual("002020142020", uut.InvocationScript.Span.ToHexString());
        }

        private static Witness MakeWitnessWithValues(int lenghtInvocation, int lengthVerification)
        {
            return new()
            {
                InvocationScript = TestUtils.GetByteArray(lenghtInvocation, 0x20),
                VerificationScript = TestUtils.GetByteArray(lengthVerification, 0x20)
            };
        }

        [TestMethod]
        public void SizeWitness_Small_Arrary()
        {
            var uut = MakeWitnessWithValues(252, 253);
            Assert.AreEqual(509, uut.Size); // (1 + 252*1) + (1 + 2 + 253*1)
        }

        [TestMethod]
        public void SizeWitness_Large_Arrary()
        {
            var uut = MakeWitnessWithValues(65535, 65536);
            Assert.AreEqual(131079, uut.Size); // (1 + 2 + 65535*1) + (1 + 4 + 65536*1)
        }

        [TestMethod]
        public void ToJson()
        {
            var uut = MakeWitnessWithValues(2, 3);
            JObject json = uut.ToJson();
            Assert.IsTrue(json.ContainsProperty("invocation"));
            Assert.IsTrue(json.ContainsProperty("verification"));
            Assert.AreEqual("ICA=", json["invocation"].AsString());
            Assert.AreEqual("ICAg", json["verification"].AsString());
        }

        [TestMethod]
        public void TestEmpty()
        {
            var w1 = Witness.Empty;
            var w2 = Witness.Empty;
            Assert.AreEqual(2, w1.Size);
            Assert.AreEqual(0, w1.InvocationScript.Length);
            Assert.AreEqual(0, w1.VerificationScript.Length);
            Assert.AreEqual(2, w2.Size);
            Assert.AreEqual(0, w2.InvocationScript.Length);
            Assert.AreEqual(0, w2.VerificationScript.Length);
            Assert.IsFalse(ReferenceEquals(w1, w2));
        }
    }
}
