// Copyright (C) 2015-2025 The Neo Project.
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
using Neo.VM;
using System;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    public partial class UT_ApplicationEngine
    {
        [TestMethod]
        public void TestHasKeyWithDifferentHF()
        {
            var script = BuildHasKeyLargeIndexScript();
            const uint EchidnaEnable = 10u;
            const uint FaunEnable = 20u;
            // Hardfork heights:
            // Echidna at 10, Faun at 20
            //  - index=5 => pre-Echidna (NotEchidnaJumpTable)
            //  - index=15 => Echidna enabled, Faun NOT enabled (NotFaunJumpTable)
            //  - index=30 => Faun enabled (DefaultJumpTable)
            var settings = ProtocolSettings.Default with
            {
                Hardforks = ProtocolSettings.Default.Hardforks
                    .SetItem(Hardfork.HF_Echidna, EchidnaEnable)
                    .SetItem(Hardfork.HF_Faun, FaunEnable)
            };

            Assert.IsFalse(settings.IsHardforkEnabled(Hardfork.HF_Echidna, 5u));
            Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Echidna, 15u));
            Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Echidna, 30u));
            Assert.IsFalse(settings.IsHardforkEnabled(Hardfork.HF_Faun, 15u));
            Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Faun, 30u));

            // Case A: pre-Echidna => Overflow
            ExecuteAndAssertFault<OverflowException>(script, settings, index: 5u);

            // Case B: Echidna enabled but pre-Faun => Overflow
            ExecuteAndAssertFault<OverflowException>(script, settings, index: 15u);

            // Case C: Faun enabled => InvalidOperationException
            ExecuteAndAssertFault<OverflowException>(script, settings, index: 30u);
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

            Assert.AreEqual(VMState.FAULT, engine.State, $"Expected FAULT at index={index}.");
            Assert.IsNotNull(engine.FaultException, $"Expected FaultException at index={index}.");
            Assert.IsInstanceOfType(engine.FaultException, typeof(TException),
                $"Expected {typeof(TException).Name} at index={index}, but got {engine.FaultException.GetType().Name}.");
        }
    }
}
