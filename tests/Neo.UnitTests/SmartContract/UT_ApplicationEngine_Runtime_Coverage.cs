// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine_Runtime_Coverage.cs file belongs to the neo project and is free
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
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine_Runtime_Coverage
    {
        private DataCache _snapshot;

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        [TestMethod]
        public void BurnGas_Positive_IncreasesFee_NegativeOrZero_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            var before = engine.FeeConsumed;
            engine.BurnGas(10_000);
            Assert.IsTrue(engine.FeeConsumed > before);
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.BurnGas(0));
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.BurnGas(-1));
        }

        [TestMethod]
        public void CheckWitness_InvalidLength_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.ThrowsExactly<ArgumentException>(() => engine.CheckWitness(new byte[5]));
            Assert.ThrowsExactly<ArgumentException>(() => engine.CheckWitness(new byte[32]));
        }

        [TestMethod]
        public void CheckWitness_WithTransactionSigner_ReturnsTrue()
        {
            var account = UInt160.Parse("0x0000000000000000000000000000000000000001");
            var tx = TestEngineRunner.EmptyTx(account);
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP }, tx);
            Assert.IsTrue(engine.CheckWitness(account.ToArray()));
            Assert.IsFalse(engine.CheckWitness(UInt160.Zero.ToArray()));
        }

        [TestMethod]
        public void CheckWitness_NullContainer_ReturnsFalse()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.IsFalse(engine.CheckWitness(UInt160.Zero.ToArray()));
        }

        [TestMethod]
        public void GetCurrentSigners_WithAndWithoutTransaction()
        {
            using (var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP }))
            {
                Assert.IsNull(engine.GetCurrentSigners());
            }

            var tx = TestEngineRunner.EmptyTx(UInt160.Zero);
            using var engineTx = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP }, tx);
            var signers = engineTx.GetCurrentSigners();
            Assert.IsNotNull(signers);
            Assert.HasCount(1, signers);
            Assert.AreEqual(UInt160.Zero, signers[0].Account);
        }

        [TestMethod]
        public void GetCallFlags_ReturnsContextFlags()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.AreEqual(CallFlags.All, engine.GetCallFlags());
        }

        [TestMethod]
        public void GetInvocationCounter_StartsAtOne()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.AreEqual(1, engine.GetInvocationCounter());
            Assert.AreEqual(1, engine.GetInvocationCounter());
        }

        [TestMethod]
        public void GetRandom_ReturnsNonNegative()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP }, gas: 100_0000_0000);
            var a = engine.GetRandom();
            var b = engine.GetRandom();
            Assert.IsTrue(a >= 0);
            Assert.IsTrue(b >= 0);
        }

        [TestMethod]
        public void RuntimeLog_InvalidUtf8_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.ThrowsExactly<ArgumentException>(() => engine.RuntimeLog([0xFF, 0xFE]));
        }

        [TestMethod]
        public void RuntimeLoadScript_InvalidCallFlags_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                engine.RuntimeLoadScript([(byte)OpCode.RET], (CallFlags)0xFF, new Array()));
        }

        [TestMethod]
        public void GetPlatform_And_Trigger()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.AreEqual("NEO", ApplicationEngine.GetPlatform());
            Assert.AreEqual(TriggerType.Application, engine.Trigger);
        }

        [TestMethod]
        public void GetScriptContainer_WithoutInteroperable_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.GetScriptContainer());
        }
    }
}
