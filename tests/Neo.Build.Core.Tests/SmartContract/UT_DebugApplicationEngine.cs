// Copyright (C) 2015-2025 The Neo Project.
//
// UT_DebugApplicationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Builders;
using Neo.Build.Core.SmartContract;
using Neo.Build.Core.Tests.Helpers;
using Neo.Builders;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;

namespace Neo.Build.Core.Tests.SmartContract
{
    [TestClass]
    public class UT_DebugApplicationEngine
    {
        [TestMethod]
        public void TestBreakPoints()
        {
            var pb = BlockBuilder
                .CreateNext(TestNode.NeoSystem.GenesisBlock)
                .Build();

            using var sb = new ScriptBuilder()
                .Emit(OpCode.NOP)
                .Emit(OpCode.NOP)
                .Emit(OpCode.NOP)
                .Emit(OpCode.NOP);

            var tx = TransactionBuilder.CreateEmpty()
                .AttachSystem(sb.ToArray())
                .Build();

            using var debugger = new DebugApplicationEngine(
                TestNode.NeoSystem.Settings,
                TestNode.NeoSystem.StoreView,
                loggerFactory: TestDefaults.FactoryLogger,
                container: tx,
                persistingBlock: pb);

            debugger.LoadScript(tx.Script);

            Assert.IsNotNull(debugger.CurrentContext);
            Assert.IsFalse(debugger.RemoveBreakPoints(debugger.CurrentContext.Script, pb.Index, tx.Hash, 3));

            Assert.IsNotNull(debugger.CurrentContext.NextInstruction);
            Assert.AreEqual(OpCode.NOP, debugger.CurrentContext.NextInstruction.OpCode);

            debugger.AddBreakPoints(debugger.CurrentContext.Script, pb.Index, tx.Hash, 2);
            debugger.AddBreakPoints(debugger.CurrentContext.Script, pb.Index, tx.Hash, 3);
            debugger.Execute();

            Assert.IsNotNull(debugger.CurrentContext);
            Assert.IsNotNull(debugger.CurrentContext.NextInstruction);

            Assert.AreEqual(OpCode.NOP, debugger.CurrentContext.NextInstruction.OpCode);
            Assert.AreEqual(2, debugger.CurrentContext.InstructionPointer);
            Assert.AreEqual(VMState.BREAK, debugger.State);

            Assert.IsFalse(debugger.RemoveBreakPoints(debugger.CurrentContext.Script, pb.Index, tx.Hash, 4));
            Assert.IsTrue(debugger.RemoveBreakPoints(debugger.CurrentContext.Script, pb.Index, tx.Hash, 2));
            Assert.IsTrue(debugger.RemoveBreakPoints(debugger.CurrentContext.Script, pb.Index, tx.Hash, 3));
            Assert.IsFalse(debugger.RemoveBreakPoints(debugger.CurrentContext.Script, pb.Index, tx.Hash, 2));
            Assert.IsFalse(debugger.RemoveBreakPoints(debugger.CurrentContext.Script, pb.Index, tx.Hash, 3));

            debugger.Execute();

            Assert.AreEqual(VMState.HALT, debugger.State);
        }

        [TestMethod]
        public void TestDebugSnapshotStorage()
        {
            var pb = BlockBuilder
                .CreateNext(TestNode.NeoSystem.GenesisBlock)
                .Build();

            using var sb = new ScriptBuilder()
                .EmitDynamicCall(NativeContract.NEO.Hash, "transfer", NativeContract.NEO.Hash, NativeContract.NEO.Hash, 324, null);

            using var debugger = new DebugApplicationEngine(
                TestNode.NeoSystem.Settings,
                TestNode.NeoSystem.StoreView,
                loggerFactory: TestDefaults.FactoryLogger,
                persistingBlock: pb);

            debugger.LoadScript(sb.ToArray());

            var actualState = debugger.Execute();

            Console.WriteLine("{0}", sb.ToArray().ToScriptHash());

            Assert.AreEqual(VMState.HALT, actualState);
            Assert.AreEqual(1, debugger.SnapshotStack.Count);
        }
    }
}
