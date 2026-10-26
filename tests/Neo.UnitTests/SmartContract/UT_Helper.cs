// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Helper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Extensions.Factories;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Neo.SmartContract.Helper;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Helper
    {
        private KeyPair _key;
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var pk = RandomNumberFactory.NextBytes(32);
            _key = new KeyPair(pk);
        }

        [TestMethod]
        public void VerifyWitnesses_NullWitnessReturnsFalse()
        {
            var payload = new ExtensiblePayload { Category = "dBFT", Sender = UInt160.Zero, Witness = null! };
            Assert.IsFalse(payload.VerifyWitnesses(TestProtocolSettings.Default, _snapshotCache, 150000000));
            Assert.IsFalse(payload.VerifyWitness(TestProtocolSettings.Default, _snapshotCache, UInt160.Zero, null!, 150000000, out var fee));
            Assert.AreEqual(0L, fee);
        }

        [TestMethod]
        public void TestGetContractHash()
        {
            var nef = new NefFile()
            {
                Compiler = "test",
                Source = string.Empty,
                Tokens = Array.Empty<MethodToken>(),
                Script = new byte[] { 1, 2, 3 }
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);

            Assert.AreEqual("0x9b9628e4f1611af90e761eea8cc21372380c74b6", GetContractHash(UInt160.Zero, nef.CheckSum, "").ToString());
            Assert.AreEqual("0x66eec404d86b918d084e62a29ac9990e3b6f4286", GetContractHash(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"), nef.CheckSum, "").ToString());
        }

        [TestMethod]
        public void TestIsMultiSigContract()
        {
            var case1 = new byte[]
            {
                0, 2, 12, 33, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221,
                221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 12, 33, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 0,
            };
            Assert.IsFalse(IsMultiSigContract(case1));

            var case2 = new byte[]
            {
                18, 12, 33, 2, 111, 240, 59, 148, 146, 65, 206, 29, 173, 212, 53, 25, 230, 150, 14, 10, 133, 180, 26,
                105, 160, 92, 50, 129, 3, 170, 43, 206, 21, 148, 202, 22, 12, 33, 2, 111, 240, 59, 148, 146, 65, 206,
                29, 173, 212, 53, 25, 230, 150, 14, 10, 133, 180, 26, 105, 160, 92, 50, 129, 3, 170, 43, 206, 21, 148,
                202, 22, 18
            };
            Assert.IsFalse(IsMultiSigContract(case2));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(4)]
        public void TestIsMultiSigContract_InvalidPublicKey(int prefix)
        {
            var publicKey = new byte[33];
            publicKey[0] = (byte)prefix;
            using var sb = new ScriptBuilder();
            sb.EmitPush(1);
            sb.EmitPush(publicKey);
            sb.EmitPush(1);
            sb.EmitSysCall(ApplicationEngine.System_Crypto_CheckMultisig);
            var script = sb.ToArray();

            Assert.IsFalse(IsMultiSigContract(script, out _, out ECPoint[] _));
            Assert.IsFalse(IsMultiSigContract(script, out int _, out int _));
            Assert.IsFalse(IsMultiSigContract(script));
        }

        [TestMethod]
        // TestIsMultiSigContract_WrongCurve checks that multisignature verification script based on points
        // not from Secp256r1 curve fails IsMultiSigContract check without any exception.
        public void TestIsMultiSigContract_WrongCurve()
        {
            // A set of points on Koblitz curve in uncompressed representation. One of the points
            // (the first one) is specially selected, this point can't be restored on Secp256r1
            // from compressed form, whereas three other points can be restored on both Secp256r1
            // and Koblitz curves.
            var pubs = new List<ECPoint>()
            {
                ECPoint.Parse("047b4e72ae854b6a0955b3e02d92651ab7fa641a936066776ad438f95bb674a269a63ff98544691663d91a6cfcd215831f01bfb7a226363a6c5c67ef14541dba07", ECCurve.Secp256k1),
                ECPoint.Parse("040486468683c112125978ffe876245b2006bfe739aca8539b67335079262cb27ad0dedc9e5583f99b61c6f46bf80b97eaec3654b87add0e5bd7106c69922a229d", ECCurve.Secp256k1),
                ECPoint.Parse("040d26fc2ad3b1aae20f040b5f83380670f8ef5c2b2ac921ba3bdd79fd0af0525177715fd4370b1012ddd10579698d186ab342c223da3e884ece9cab9b6638c7bb", ECCurve.Secp256k1),
                ECPoint.Parse("04a114d72fe2997cdac67427b6f39ea08ed46213c8bb6a461bbac2a6212cf43fb510f8adf59b0b087a7859f96d0288e5e94800eab8388f30f03f92b2e4d807dfce", ECCurve.Secp256k1)
            };
            const int m = 3;

            var badScript = Contract.CreateMultiSigRedeemScript(m, pubs);
            Assert.IsFalse(IsMultiSigContract(badScript, out _, out ECPoint[] _));
            Assert.IsFalse(IsMultiSigContract(badScript));
            Assert.IsFalse(IsMultiSigContract(badScript, out int _, out int _));

            // Exclude the first special point and check one more time; all overloads should return true.
            var goodScript = Contract.CreateMultiSigRedeemScript(m, pubs.Skip(1).ToArray());
            Assert.IsTrue(IsMultiSigContract(goodScript, out _, out ECPoint[] _));
            Assert.IsTrue(IsMultiSigContract(goodScript));
            Assert.IsTrue(IsMultiSigContract(goodScript, out int _, out int _));
        }

        [TestMethod]
        // TestIsSignatureContract_WrongCurve checks that signature verification script based on point
        // not from Secp256r1 curve passes IsSignatureContract check without any exception.
        public void TestIsSignatureContract_WrongCurve()
        {
            // A special point on Koblitz curve that can't be restored at Secp256r1 from compressed form.
            var pub = ECPoint.Parse("047b4e72ae854b6a0955b3e02d92651ab7fa641a936066776ad438f95bb674a269a63ff98544691663d91a6cfcd215831f01bfb7a226363a6c5c67ef14541dba07", ECCurve.Secp256k1);
            var script = Contract.CreateSignatureRedeemScript(pub);

            // IsSignatureContract should pass since it doesn't perform ECPoint decoding.
            Assert.IsTrue(IsSignatureContract(script));
        }

        [TestMethod]
        public void TestSignatureContractCost()
        {
            var snapshot = _snapshotCache.CloneCache();
            var contract = Contract.CreateSignatureContract(_key.PublicKey);

            var tx = TestUtils.CreateRandomHashTransaction();
            tx.Signers[0].Account = contract.ScriptHash;

            using ScriptBuilder invocationScript = new();
            invocationScript.EmitPush(Neo.Wallets.Helper.Sign(tx, _key, TestProtocolSettings.Default.Network));
            tx.Witnesses = new Witness[] { new Witness() { InvocationScript = invocationScript.ToArray(), VerificationScript = contract.Script } };

            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Gorgon, 1).SetItem(Hardfork.HF_Huyao, uint.MaxValue)
            };
            using var engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, null, settings);
            engine.LoadScript(contract.Script);
            engine.LoadScript(new Script(invocationScript.ToArray(), true), configureState: p => p.CallFlags = CallFlags.None);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());

            Assert.AreEqual(SignatureContractCost() * PolicyContract.DefaultExecFeeFactor, engine.FeeConsumed);
        }

        [TestMethod]
        public void TestMultiSignatureContractCost()
        {
            var snapshot = _snapshotCache.CloneCache();
            var contract = Contract.CreateMultiSigContract(1, new ECPoint[] { _key.PublicKey });

            var tx = TestUtils.CreateRandomHashTransaction();
            tx.Signers[0].Account = contract.ScriptHash;

            using ScriptBuilder invocationScript = new();
            invocationScript.EmitPush(Neo.Wallets.Helper.Sign(tx, _key, TestProtocolSettings.Default.Network));

            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Gorgon, 1).SetItem(Hardfork.HF_Huyao, uint.MaxValue)
            };
            using var engine = ApplicationEngine.Create(TriggerType.Verification, tx, snapshot, null, settings);
            engine.LoadScript(contract.Script);
            engine.LoadScript(new Script(invocationScript.ToArray(), true), configureState: p => p.CallFlags = CallFlags.None);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());

            Assert.AreEqual(MultiSignatureContractCost(1, 1) * PolicyContract.DefaultExecFeeFactor, engine.FeeConsumed);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void TestContractCostWithFractionalExecFeeFactor(bool huyao)
        {
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, huyao ? 0 : uint.MaxValue)
            };
            // 30.5001 with the decimals defined in ApplicationEngine.FeeFactor.
            BigInteger execFeeFactor = 305001;

            // Before Huyao: (8.000 * 2 + 0 + 32768.000) * 30.5001 = 999915.2784000, rounded up.
            // Since Huyao: (1.685 * 2 + 0 + 32768.000) * 30.5001 = 999530.0621370, rounded up.
            Assert.AreEqual(huyao ? 999531 : 999916, SignatureContractCost(settings, 1, execFeeFactor));
            // Before Huyao: (8.000 * 5 + 1.000 * 2 + 0 + 32768.000 * 3) * 30.5001 = 2999562.8346000, rounded up.
            // Since Huyao: (1.685 * 5 + 2.187 * 2 + 0 + 32768.000 * 3) * 30.5001 = 2998672.2011799, rounded up.
            Assert.AreEqual(huyao ? 2998673 : 2999563, MultiSignatureContractCost(settings, 1, execFeeFactor, 2, 3));
            if (!huyao)
            {
                // An integer factor gives the same result as multiplying by it.
                Assert.AreEqual(30 * SignatureContractCost(), SignatureContractCost(settings, 1, 30 * ApplicationEngine.FeeFactor));
                Assert.AreEqual(30 * MultiSignatureContractCost(2, 3), MultiSignatureContractCost(settings, 1, 30 * ApplicationEngine.FeeFactor, 2, 3));
            }
        }

        [TestMethod]
        [DataRow(false, 9u, 300000)]
        [DataRow(false, 10u, 300000)]
        [DataRow(false, 11u, 300000)]
        [DataRow(true, 9u, 300000)]
        [DataRow(true, 10u, 300000)]
        [DataRow(true, 11u, 300000)]
        [DataRow(false, 9u, 305001)]
        [DataRow(false, 10u, 305001)]
        [DataRow(true, 9u, 305001)]
        [DataRow(true, 10u, 305001)]
        public void TestContractCostMatchesWitnessExecutionAcrossHuyao(bool multisig, uint verificationHeight, int execFeeFactor)
        {
            var snapshot = _snapshotCache.CloneCache();
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Faun, 0).SetItem(Hardfork.HF_Huyao, 10)
            };
            snapshot.GetAndChange(StorageKey.Create(NativeContract.Ledger.Id, 12))!
                .GetInteroperable<HashIndexState>().Index = verificationHeight - 1;
            snapshot.GetAndChange(StorageKey.Create(NativeContract.Policy.Id, 18))!.Set(execFeeFactor);

            var keys = Enumerable.Range(1, multisig ? 3 : 1).Select(i =>
            {
                var privateKey = new byte[32];
                privateKey[^1] = (byte)i;
                return new KeyPair(privateKey);
            }).OrderBy(key => key.PublicKey).ToArray();
            var verification = multisig
                ? Contract.CreateMultiSigRedeemScript(2, keys.Select(key => key.PublicKey).ToArray())
                : Contract.CreateSignatureRedeemScript(keys[0].PublicKey);
            var account = verification.ToScriptHash();
            var tx = new Transaction
            {
                Script = new[] { (byte)OpCode.PUSH1 },
                Signers = [new Signer { Account = account, Scopes = WitnessScope.CalledByEntry }],
                Attributes = [],
                ValidUntilBlock = verificationHeight,
                Witnesses = []
            };

            Witness SignWitness()
            {
                using var builder = new ScriptBuilder();
                foreach (var key in keys.Take(multisig ? 2 : 1))
                    builder.EmitPush(Neo.Wallets.Helper.Sign(tx, key, settings.Network));
                return new Witness { InvocationScript = builder.ToArray(), VerificationScript = verification };
            }

            tx.Witnesses = [SignWitness()];
            Assert.IsTrue(tx.VerifyWitness(settings, snapshot, account, tx.Witnesses[0], ApplicationEngine.TestModeGas, out var executionFee));

            var requiredFee = executionFee + tx.Size * NativeContract.Policy.GetFeePerByte(snapshot);
            tx.NetworkFee = requiredFee;
            tx.Witnesses = [SignWitness()];
            Assert.IsTrue(tx.VerifyWitness(settings, snapshot, account, tx.Witnesses[0], executionFee, out var finalExecutionFee));
            Assert.AreEqual(executionFee, finalExecutionFee);

            Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateDependent(settings, snapshot, null!, []));
            tx.NetworkFee--;
            tx.Witnesses = [SignWitness()];
            Assert.AreEqual(VerifyResult.InsufficientFunds, tx.VerifyStateDependent(settings, snapshot, null!, []));
            Assert.AreEqual(requiredFee, tx.CalculateNetworkFee(snapshot, settings, hash => hash.Equals(account) ? verification : null));
        }
    }
}
