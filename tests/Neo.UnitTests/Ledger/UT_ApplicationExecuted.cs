// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationExecuted.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.VM;
using System;
using Buffer = Neo.VM.Types.Buffer;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_ApplicationExecuted
    {
        [TestMethod]
        public void FromEngine_Halt_WithoutTransaction()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = ApplicationEngine.Run(new byte[] { (byte)OpCode.PUSH1 }, snapshot);
            Assert.AreEqual(VMState.HALT, engine.State);

            var executed = new Blockchain.ApplicationExecuted(engine);
            Assert.IsNull(executed.Transaction);
            Assert.AreEqual(TriggerType.Application, executed.Trigger);
            Assert.AreEqual(VMState.HALT, executed.VMState);
            Assert.IsNull(executed.Exception);
            Assert.IsTrue(executed.GasConsumed >= 0);
            Assert.HasCount(1, executed.Stack);
            Assert.IsEmpty(executed.Notifications);
        }

        [TestMethod]
        public void FromEngine_NewBuffer_PinsPooledMemory()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var sb = new ScriptBuilder();
            sb.EmitPush(1);
            sb.Emit(OpCode.NEWBUFFER);
            using var engine = ApplicationEngine.Run(sb.ToArray(), snapshot);
            Assert.AreEqual(VMState.HALT, engine.State);

            var executed = new Blockchain.ApplicationExecuted(engine);
            Assert.HasCount(1, executed.Stack);
            Assert.IsInstanceOfType<Buffer>(executed.Stack[0]);
            Assert.AreEqual(1, executed.Stack[0].GetSpan().Length);
        }

        [TestMethod]
        public void FromEngine_Fault_CapturesException()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = ApplicationEngine.Run(new byte[] { (byte)OpCode.ABORT }, snapshot);
            Assert.AreEqual(VMState.FAULT, engine.State);

            var executed = new Blockchain.ApplicationExecuted(engine);
            Assert.AreEqual(VMState.FAULT, executed.VMState);
            Assert.IsNotNull(executed.Exception);
            Assert.IsInstanceOfType<Exception>(executed.Exception);
        }

        [TestMethod]
        public void FromEngine_WithTransactionContainer()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var tx = new Transaction
            {
                Version = 0,
                Nonce = 1,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 100,
                Attributes = [],
                Signers = [new Signer { Account = UInt160.Zero, Scopes = WitnessScope.None }],
                Script = new byte[] { (byte)OpCode.RET },
                Witnesses = []
            };
            using var engine = ApplicationEngine.Run(new byte[] { (byte)OpCode.RET }, snapshot, container: tx);
            var executed = new Blockchain.ApplicationExecuted(engine);
            Assert.AreSame(tx, executed.Transaction);
        }
    }
}
