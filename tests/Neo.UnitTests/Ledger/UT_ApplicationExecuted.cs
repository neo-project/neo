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
using Neo.VM.Types;
using System;
using System.Reflection;
using Array = Neo.VM.Types.Array;
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
            sb.EmitPush(new byte[] { 0xA5 });
            sb.Emit(OpCode.CONVERT, [(byte)StackItemType.Buffer]);
            Blockchain.ApplicationExecuted executed;
            using (var engine = ApplicationEngine.Run(sb.ToArray(), snapshot))
            {
                Assert.AreEqual(VMState.HALT, engine.State);
                executed = new Blockchain.ApplicationExecuted(engine);
            }

            Assert.HasCount(1, executed.Stack);
            Assert.IsInstanceOfType<Buffer>(executed.Stack[0]);
            InvokeCleanup(executed.Stack[0]);
            Assert.AreEqual((byte)0xA5, executed.Stack[0].GetSpan()[0]);
        }

        [TestMethod]
        public void FromEngine_CyclicArray_KeepDoesNotStackOverflow()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            byte[] script =
            [
                (byte)OpCode.NEWARRAY0,
                (byte)OpCode.DUP,
                (byte)OpCode.DUP,
                (byte)OpCode.APPEND,
                (byte)OpCode.RET
            ];
            Blockchain.ApplicationExecuted executed;
            using (var engine = ApplicationEngine.Run(script, snapshot))
            {
                Assert.AreEqual(VMState.HALT, engine.State);
                executed = new Blockchain.ApplicationExecuted(engine);
            }

            Assert.HasCount(1, executed.Stack);
            var array = executed.Stack[0] as Array;
            Assert.IsNotNull(array);
            Assert.HasCount(1, array);
            Assert.AreSame(array, array[0]);
        }

        [TestMethod]
        public void FromEngine_NestedBuffer_PinsAfterDispose()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var sb = new ScriptBuilder();
            sb.EmitPush(new byte[] { 0x5A });
            sb.Emit(OpCode.CONVERT, [(byte)StackItemType.Buffer]);
            sb.EmitPush(1);
            sb.Emit(OpCode.PACK);
            sb.EmitPush(1);
            sb.Emit(OpCode.PACK);
            Blockchain.ApplicationExecuted executed;
            using (var engine = ApplicationEngine.Run(sb.ToArray(), snapshot))
            {
                Assert.AreEqual(VMState.HALT, engine.State);
                executed = new Blockchain.ApplicationExecuted(engine);
            }

            Assert.HasCount(1, executed.Stack);
            var outer = executed.Stack[0] as Array;
            Assert.IsNotNull(outer);
            var inner = outer[0] as Array;
            Assert.IsNotNull(inner);
            Assert.IsInstanceOfType<Buffer>(inner[0]);
            InvokeCleanup(inner[0]);
            Assert.AreEqual((byte)0x5A, inner[0].GetSpan()[0]);
        }

        [TestMethod]
        public void FromEngine_PushData_ByteStringSurvivesEngineDispose()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var sb = new ScriptBuilder();
            sb.EmitPush(new byte[] { 0x3C, 0x7E });
            Blockchain.ApplicationExecuted executed;
            using (var engine = ApplicationEngine.Run(sb.ToArray(), snapshot))
            {
                Assert.AreEqual(VMState.HALT, engine.State);
                executed = new Blockchain.ApplicationExecuted(engine);
            }

            Assert.HasCount(1, executed.Stack);
            Assert.IsInstanceOfType<ByteString>(executed.Stack[0]);
            var span = executed.Stack[0].GetSpan();
            Assert.AreEqual((byte)0x3C, span[0]);
            Assert.AreEqual((byte)0x7E, span[1]);
        }

        [TestMethod]
        public void FromEngine_NotifyBuffer_DeepCopyByteStringSurvivesDispose()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            Blockchain.ApplicationExecuted executed;
            using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default))
            {
                engine.LoadScript(new byte[] { (byte)OpCode.RET });
                engine.SendNotification(UInt160.Zero, "evt", new Array { new Buffer([(byte)0xA5]) });
                Assert.AreEqual(VMState.HALT, engine.Execute());
                executed = new Blockchain.ApplicationExecuted(engine);
            }

            Assert.HasCount(1, executed.Notifications);
            var state = executed.Notifications[0].State;
            Assert.HasCount(1, state);
            Assert.IsInstanceOfType<ByteString>(state[0]);
            Assert.AreEqual((byte)0xA5, state[0].GetSpan()[0]);
        }

        private static void InvokeCleanup(StackItem item)
        {
            var method = typeof(StackItem).GetMethod("Cleanup", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method.Invoke(item, null);
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
