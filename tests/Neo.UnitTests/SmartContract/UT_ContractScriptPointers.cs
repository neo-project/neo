// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractScriptPointers.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ContractScriptPointers
    {
        // getPtr: PUSHA 8, RET; callPtr: CALLA, RET; target: PUSHINT8 42, RET.
        private static readonly byte[] s_contractScript =
        [
            (byte)OpCode.PUSHA, 0x08, 0x00, 0x00, 0x00,
            (byte)OpCode.RET,
            (byte)OpCode.CALLA,
            (byte)OpCode.RET,
            (byte)OpCode.PUSHINT8, 42,
            (byte)OpCode.RET
        ];

        private static readonly ProtocolSettings s_beforeHuyao = TestProtocolSettings.Default with
        {
            Hardforks = TestProtocolSettings.Default.Hardforks
                .Where(p => p.Key < Hardfork.HF_Huyao).ToImmutableDictionary()
        };

        private static (DataCache Snapshot, UInt160 Hash) CreateContract()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var manifest = TestUtils.CreateDefaultManifest();
            manifest.Abi.Methods =
            [
                new ContractMethodDescriptor { Name = "getPtr", Parameters = [], ReturnType = ContractParameterType.Any, Offset = 0, Safe = false },
                new ContractMethodDescriptor
                {
                    Name = "callPtr",
                    Parameters = [new ContractParameterDefinition { Name = "p", Type = ContractParameterType.Any }],
                    ReturnType = ContractParameterType.Any,
                    Offset = 6,
                    Safe = false
                }
            ];
            var contract = TestUtils.GetContract(s_contractScript, manifest);
            snapshot.Add(new KeyBuilder(NativeContract.ContractManagement.Id, 8).Add(contract.Hash),
                new StorageItem(BinarySerializer.Serialize(contract.ToStackItem(), ExecutionEngineLimits.Default)));
            return (snapshot, contract.Hash);
        }

        private static ApplicationEngine Run(DataCache snapshot, ScriptBuilder script, ProtocolSettings settings)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 20_00000000);
            engine.LoadScript(script.ToArray());
            engine.Execute();
            return engine;
        }

        private static ApplicationEngine ComparePointers(ProtocolSettings settings, OpCode opcode, bool inStruct = false)
        {
            var engine = ApplicationEngine.Create(TriggerType.Application, null,
                TestBlockchain.GetTestSnapshotCache(), settings: settings, gas: 20_00000000);
            engine.LoadScript(new Script(new byte[] { (byte)opcode, (byte)OpCode.RET }));
            byte[] bytes = [(byte)OpCode.RET];
            var first = new Pointer(new Script(bytes), 0);
            var second = new Pointer(new Script(bytes), 0);
            engine.Push(inStruct ? new Neo.VM.Types.Struct([first]) : first);
            engine.Push(inStruct ? new Neo.VM.Types.Struct([second]) : second);
            engine.Execute();
            return engine;
        }

        [TestMethod]
        public void PointerFromDifferentScriptInstanceRespectsHuyao()
        {
            var script = new Script(new byte[] { (byte)OpCode.CALLA, (byte)OpCode.RET });
            using var engine = ApplicationEngine.Create(TriggerType.Application, null,
                TestBlockchain.GetTestSnapshotCache(), settings: s_beforeHuyao, gas: 20_00000000);
            engine.LoadScript(script);
            engine.Push(new Pointer(new Script((ReadOnlyMemory<byte>)script), 1));
            engine.Execute();
            Assert.AreEqual(VMState.FAULT, engine.State);
            Assert.Contains("Pointers can't be shared between scripts", engine.FaultException!.Message);

            using var after = ApplicationEngine.Create(TriggerType.Application, null,
                TestBlockchain.GetTestSnapshotCache(), settings: TestProtocolSettings.Default, gas: 20_00000000);
            after.LoadScript(script);
            after.Push(new Pointer(new Script((ReadOnlyMemory<byte>)script), 1));
            after.Execute();
            Assert.AreEqual(VMState.HALT, after.State);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ContextUnloadedCommitsSeparateScriptInstances(bool huyaoEnabled)
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot,
                settings: huyaoEnabled ? TestProtocolSettings.Default : s_beforeHuyao);
            byte[] script = [(byte)OpCode.RET];
            engine.LoadScript(new Script(script));
            var inner = engine.LoadScript(new Script(script));
            Assert.IsFalse(ReferenceEquals(inner.Script, engine.EntryContext!.Script));

            var key = StorageKey.Create(NativeContract.Policy.Id, 200, new byte[] { 1 });
            inner.GetState<ExecutionContextState>().SnapshotCache!.Add(key, new StorageItem(1u));
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.IsNotNull(snapshot.TryGet(key));
        }

        [TestMethod]
        public void PointersFromDifferentScriptInstancesCompareUnequalBeforeHuyao()
        {
            using var engine = ComparePointers(s_beforeHuyao, OpCode.EQUAL);
            Assert.AreEqual(VMState.HALT, engine.State);
            Assert.IsFalse(engine.ResultStack.Pop().GetBoolean());
        }

        [TestMethod]
        public void PointersFromSeparateContractCallsCompareEqual()
        {
            var (snapshot, hash) = CreateContract();
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(hash, "getPtr");
            script.EmitDynamicCall(hash, "getPtr");
            script.Emit(OpCode.EQUAL);

            using var before = Run(snapshot, script, s_beforeHuyao);
            Assert.AreEqual(VMState.HALT, before.State);
            Assert.IsTrue(before.ResultStack.Pop().GetBoolean());

            using var after = Run(snapshot, script, TestProtocolSettings.Default);
            Assert.AreEqual(VMState.HALT, after.State);
            Assert.IsTrue(after.ResultStack.Pop().GetBoolean());
        }

        [TestMethod]
        public void PointerFromEarlierContractCallCanBeCalled()
        {
            var (snapshot, hash) = CreateContract();
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(hash, "getPtr");
            script.EmitPush(1);
            script.Emit(OpCode.PACK);
            script.EmitPush((byte)CallFlags.All);
            script.EmitPush("callPtr");
            script.EmitPush(hash);
            script.EmitSysCall(ApplicationEngine.System_Contract_Call);

            using var before = Run(snapshot, script, s_beforeHuyao);
            Assert.AreEqual(VMState.HALT, before.State);
            Assert.AreEqual(42, (int)before.ResultStack.Pop().GetInteger());

            using var after = Run(snapshot, script, TestProtocolSettings.Default);
            Assert.AreEqual(VMState.HALT, after.State);
            Assert.AreEqual(42, (int)after.ResultStack.Pop().GetInteger());
        }

        [TestMethod]
        public void PointerComparisonsInStructsRespectHuyao()
        {
            using var before = ComparePointers(s_beforeHuyao, OpCode.EQUAL, inStruct: true);
            Assert.AreEqual(VMState.HALT, before.State);
            Assert.IsFalse(before.ResultStack.Pop().GetBoolean());

            using var after = ComparePointers(TestProtocolSettings.Default, OpCode.EQUAL, inStruct: true);
            Assert.AreEqual(VMState.HALT, after.State);
            Assert.IsTrue(after.ResultStack.Pop().GetBoolean());
        }

        [TestMethod]
        public void NotEqualRespectsHuyao()
        {
            using var before = ComparePointers(s_beforeHuyao, OpCode.NOTEQUAL);
            Assert.AreEqual(VMState.HALT, before.State);
            Assert.IsTrue(before.ResultStack.Pop().GetBoolean());

            using var after = ComparePointers(TestProtocolSettings.Default, OpCode.NOTEQUAL);
            Assert.AreEqual(VMState.HALT, after.State);
            Assert.IsFalse(after.ResultStack.Pop().GetBoolean());
        }
    }
}
