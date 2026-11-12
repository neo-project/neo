// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine.JumpTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {
        [TestMethod]
        public void TestSHR_SHL_WithDifferentHF()
        {
            foreach (var op in new OpCode[] { OpCode.SHL, OpCode.SHR })
            {
                using var sb = new ScriptBuilder();

                sb.Emit(OpCode.NEWARRAY0); // intentionally wrong argument for SHL or SHR.
                sb.EmitPush(0); // zero shift.
                sb.Emit(op);

                var script = sb.ToArray();

                const uint EchidnaEnable = 10u;
                const uint GorgonEnable = 20u;
                // Hardfork heights:
                // Echidna at 10, Gorgon at 20
                //  - index=5 => pre-Echidna (NotEchidnaJumpTable)
                //  - index=15 => Echidna enabled, Gorgon NOT enabled (NotGorgonJumpTable)
                //  - index=30 => Gorgon enabled (DefaultJumpTable)
                var settings = ProtocolSettings.Default with
                {
                    Hardforks = ProtocolSettings.Default.Hardforks
                        .SetItem(Hardfork.HF_Echidna, EchidnaEnable)
                        .SetItem(Hardfork.HF_Gorgon, GorgonEnable)
                        .Remove(Hardfork.HF_Huyao)
                };

                Assert.IsFalse(settings.IsHardforkEnabled(Hardfork.HF_Echidna, 5u));
                Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Echidna, 15u));
                Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Echidna, 30u));
                Assert.IsFalse(settings.IsHardforkEnabled(Hardfork.HF_Gorgon, 15u));
                Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Gorgon, 30u));

                // Case A: pre-Echidna => no exception.
                var engine = Execute(script, settings, index: 5u);
                Assert.AreEqual(VMState.HALT, engine.State);
                Assert.AreEqual(1, engine.ResultStack.Count);

                // Case B: Echidna enabled but pre-Gorgon => no exception.
                engine = Execute(script, settings, index: 15u);
                Assert.AreEqual(VMState.HALT, engine.State);
                Assert.AreEqual(1, engine.ResultStack.Count);

                // Case C: Gorgon enabled => InvalidCastException on attempt to convert Array to Integer.
                ExecuteAndAssertFault<InvalidCastException>(script, settings, index: 30u);
            }
        }

        [TestMethod]
        public void TestHasKeyWithDifferentHF()
        {
            var script = BuildHasKeyLargeIndexScript();
            const uint EchidnaEnable = 10u;
            const uint GorgonEnable = 20u;
            // Hardfork heights:
            // Echidna at 10, Gorgon at 20
            //  - index=5 => pre-Echidna (NotEchidnaJumpTable)
            //  - index=15 => Echidna enabled, Gorgon NOT enabled (NotGorgonJumpTable)
            //  - index=30 => Gorgon enabled (DefaultJumpTable)
            var settings = ProtocolSettings.Default with
            {
                Hardforks = ProtocolSettings.Default.Hardforks
                    .SetItem(Hardfork.HF_Echidna, EchidnaEnable)
                    .SetItem(Hardfork.HF_Gorgon, GorgonEnable)
                    .Remove(Hardfork.HF_Huyao)
            };

            Assert.IsFalse(settings.IsHardforkEnabled(Hardfork.HF_Echidna, 5u));
            Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Echidna, 15u));
            Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Echidna, 30u));
            Assert.IsFalse(settings.IsHardforkEnabled(Hardfork.HF_Gorgon, 15u));
            Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Gorgon, 30u));

            // Case A: pre-Echidna => Overflow
            ExecuteAndAssertFault<OverflowException>(script, settings, index: 5u);

            // Case B: Echidna enabled but pre-Gorgon => Overflow
            ExecuteAndAssertFault<OverflowException>(script, settings, index: 15u);

            // Case C: Gorgon enabled => InvalidOperationException
            ExecuteAndAssertFault<InvalidOperationException>(script, settings, index: 30u);
        }

        [TestMethod]
        public void TestSETITEM_WithDifferentHF()
        {
            using var sb = new ScriptBuilder();

            sb.Emit(OpCode.PUSH1);
            sb.Emit(OpCode.NEWBUFFER);
            sb.Emit(OpCode.DUP);
            sb.Emit(OpCode.PUSH0);
            sb.Emit(OpCode.PUSHM1);
            sb.Emit(OpCode.SETITEM);

            var script = sb.ToArray();

            const uint GorgonEnable = 10u;
            const uint HuyaoEnable = 20u;
            // Hardfork heights:
            // Gorgon at 10, Huyao at 20
            //  - index=5 => pre-Gorgon (NotGorgonJumpTable)
            //  - index=15 => Gorgon enabled, Huyao not enabled (NotHuyaoJumpTable)
            //  - index=30 => Huyao enabled (DefaultJumpTable)
            var settings = ProtocolSettings.Default with
            {
                Hardforks = ProtocolSettings.Default.Hardforks
                    .SetItem(Hardfork.HF_Gorgon, GorgonEnable)
                    .SetItem(Hardfork.HF_Huyao, HuyaoEnable)
            };

            Assert.IsFalse(settings.IsHardforkEnabled(Hardfork.HF_Gorgon, 5u));
            Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Gorgon, 15u));
            Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Gorgon, 30u));
            Assert.IsFalse(settings.IsHardforkEnabled(Hardfork.HF_Huyao, 15u));
            Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Huyao, 30u));

            // Case A: pre-Gorgon => no exception.
            var engine = Execute(script, settings, index: 5u);
            Assert.AreEqual(VMState.HALT, engine.State);
            Assert.AreEqual(1, engine.ResultStack.Count);

            // Case B: Gorgon enabled but pre-Huyao => exception.
            ExecuteAndAssertFault<OverflowException>(script, settings, index: 15u);

            // Case C: Huyao enabled: no exception.
            engine = Execute(script, settings, index: 30u);
            Assert.AreEqual(VMState.HALT, engine.State);
            Assert.AreEqual(1, engine.ResultStack.Count);
        }

        [TestMethod]
        // Before Huyao: (1 << 15) * 30 = 983040 datoshi.
        [DataRow(false, "native", 983040L)]
        [DataRow(false, "contract", 983040L)]
        [DataRow(false, "missing", 983040L)]
        [DataRow(false, "invalidFlags", 983040L)]
        // 2205 * 30 = 66150 datoshi.
        [DataRow(true, "native", 66150L)]
        // (2205 + 6799.366) * 30 = 270130.98 datoshi, rounded up to 270131.
        [DataRow(true, "contract", 270131L)]
        [DataRow(true, "missing", 270131L)]
        // Invalid caller flags are rejected before charging the call.
        [DataRow(true, "invalidFlags", 0L)]
        public void CallT_Price(bool huyao, string scenario, long expectedFee)
        {
            var snapshot = _snapshotCache.CloneCache();
            var callee = TestUtils.GetContract([(byte)OpCode.RET],
                TestUtils.CreateManifest("foo", ContractParameterType.Void));
            snapshot.DeleteContract(callee.Hash);
            snapshot.AddContract(callee.Hash, callee);

            var token = new MethodToken
            {
                Hash = scenario switch
                {
                    "native" => NativeContract.NEO.Hash,
                    "missing" => UInt160.Zero,
                    _ => callee.Hash
                },
                Method = scenario == "native" ? "symbol" : "foo",
                ParametersCount = 0,
                HasReturnValue = scenario == "native",
                CallFlags = CallFlags.All
            };
            using var script = new ScriptBuilder();
            script.Emit(OpCode.CALLT, new byte[] { 0, 0 });
            var caller = TestUtils.GetContract(script.ToArray(),
                TestUtils.CreateManifest("main", ContractParameterType.Void));
            caller.Nef.Tokens = [token];
            caller.Nef.CheckSum = NefFile.ComputeChecksum(caller.Nef);

            var settings = TestProtocolSettings.Default with
            {
                Hardforks = huyao
                    ? TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, 0)
                    : TestProtocolSettings.Default.Hardforks.Remove(Hardfork.HF_Huyao)
            };
            using var engine = ApplicationEngine.Create(
                TriggerType.Application, null, snapshot, settings: settings);
            engine.LoadContract(caller, caller.Manifest.Abi.GetMethod("main", 0),
                scenario == "invalidFlags" ? CallFlags.ReadStates : CallFlags.All);

            new Debugger(engine).StepInto(); // Charge CALLT without executing the callee.

            Assert.AreEqual(expectedFee, engine.FeeConsumed);
            Assert.AreEqual(
                scenario is "missing" or "invalidFlags" ? VMState.FAULT : VMState.BREAK,
                engine.State, engine.FaultException?.ToString());
            if (scenario == "missing")
                Assert.Contains("Called Contract Does Not Exist", engine.FaultException.GetBaseException().Message);
            else if (scenario == "invalidFlags")
                Assert.Contains("Cannot call this SYSCALL", engine.FaultException.GetBaseException().Message);
            else
                Assert.AreEqual(token.Hash, engine.CurrentScriptHash);
        }

        private static byte[] BuildHasKeyLargeIndexScript()
        {
            // HASKEY pops: key (PrimitiveType), then x (Array/Map/Buffer/ByteString)
            // So push x first, then key.
            var largeIndex = new BigInteger((long)int.MaxValue + 1);

            using var sb = new ScriptBuilder();

            // Build array: [1]
            sb.EmitPush(1);
            sb.EmitPush(1);
            sb.Emit(OpCode.PACK);

            // Push key and call HASKEY
            sb.EmitPush(largeIndex);
            sb.Emit(OpCode.HASKEY);

            sb.Emit(OpCode.RET);
            return sb.ToArray();
        }

        private static void ExecuteAndAssertFault<TException>(byte[] script, ProtocolSettings settings, uint index) where TException : Exception
        {
            var engine = Execute(script, settings, index);

            Assert.AreEqual(VMState.FAULT, engine.State, $"Expected FAULT at index={index}.");
            Assert.IsNotNull(engine.FaultException, $"Expected FaultException at index={index}.");
            Assert.IsInstanceOfType(engine.FaultException, typeof(TException),
                $"Expected {typeof(TException).Name} at index={index}, but got {engine.FaultException.GetType().Name}.");
        }

        private static ApplicationEngine Execute(byte[] script, ProtocolSettings settings, uint index)
        {
            var snapshotCache = TestBlockchain.GetTestSnapshotCache();
            var block = new Block
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = index,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };

            using var engine = ApplicationEngine.Create(
                TriggerType.Application,
                container: null,
                snapshotCache,
                persistingBlock: block,
                settings: settings,
                gas: 100_00000000L);

            engine.LoadScript(script);
            engine.Execute();

            return engine;
        }
    }
}
