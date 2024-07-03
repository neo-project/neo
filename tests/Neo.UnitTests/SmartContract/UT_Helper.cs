// Copyright (C) 2015-2024 The Neo Project.
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
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using static Neo.SmartContract.Helper;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Helper
    {
        private KeyPair _key;

        [TestInitialize]
        public void Init()
        {
            var pk = new byte[32];
            new Random().NextBytes(pk);
            _key = new KeyPair(pk);
        }

        [TestMethod]
        public void TestGetContractHash()
        {
            var nef = new NefFile()
            {
                Compiler = "test",
                Source = string.Empty,
                Tokens = [],
                Script = new byte[] { 1, 2, 3 }
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);

            Assert.AreEqual("0x9b9628e4f1611af90e761eea8cc21372380c74b6", Neo.SmartContract.Helper.GetContractHash(UInt160.Zero, nef.CheckSum, "").ToString());
            Assert.AreEqual("0x66eec404d86b918d084e62a29ac9990e3b6f4286", Neo.SmartContract.Helper.GetContractHash(UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"), nef.CheckSum, "").ToString());
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
            Assert.IsFalse(IsMultiSigContract(badScript, out _, out ECPoint[] _)); // enforce runtime point decoding by specifying ECPoint[] out variable.
            Assert.IsTrue(IsMultiSigContract(badScript)); // this overload is unlucky since it doesn't perform ECPoint decoding.

            // Exclude the first special point and check one more time, both methods should return true.
            var goodScript = Contract.CreateMultiSigRedeemScript(m, pubs.Skip(1).ToArray());
            Assert.IsTrue(IsMultiSigContract(goodScript, out _, out ECPoint[] _)); // enforce runtime point decoding by specifying ECPoint[] out variable.
            Assert.IsTrue(IsMultiSigContract(goodScript)); // this overload is unlucky since it doesn't perform ECPoint decoding.
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
            var contract = Contract.CreateSignatureContract(_key.PublicKey);

            var tx = TestUtils.CreateRandomHashTransaction();
            tx.Signers[0].Account = contract.ScriptHash;

            using ScriptBuilder invocationScript = new();
            invocationScript.EmitPush(Neo.Wallets.Helper.Sign(tx, _key, TestProtocolSettings.Default.Network));
            tx.Witnesses = [new Witness() { InvocationScript = invocationScript.ToArray(), VerificationScript = contract.Script }
            ];

            using var engine = ApplicationEngine.Create(TriggerType.Verification, tx, null, null, TestProtocolSettings.Default);
            engine.LoadScript(contract.Script);
            engine.LoadScript(new Script(invocationScript.ToArray(), true), configureState: p => p.CallFlags = CallFlags.None);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());

            Assert.AreEqual(Neo.SmartContract.Helper.SignatureContractCost() * PolicyContract.DefaultExecFeeFactor, engine.FeeConsumed);
        }

        [TestMethod]
        public void TestMultiSignatureContractCost()
        {
            var contract = Contract.CreateMultiSigContract(1, new ECPoint[] { _key.PublicKey });

            var tx = TestUtils.CreateRandomHashTransaction();
            tx.Signers[0].Account = contract.ScriptHash;

            using ScriptBuilder invocationScript = new();
            invocationScript.EmitPush(Neo.Wallets.Helper.Sign(tx, _key, TestProtocolSettings.Default.Network));

            using var engine = ApplicationEngine.Create(TriggerType.Verification, tx, null, null, TestProtocolSettings.Default);
            engine.LoadScript(contract.Script);
            engine.LoadScript(new Script(invocationScript.ToArray(), true), configureState: p => p.CallFlags = CallFlags.None);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.IsTrue(engine.ResultStack.Pop().GetBoolean());

            Assert.AreEqual(Neo.SmartContract.Helper.MultiSignatureContractCost(1, 1) * PolicyContract.DefaultExecFeeFactor, engine.FeeConsumed);
        }
    }
}
