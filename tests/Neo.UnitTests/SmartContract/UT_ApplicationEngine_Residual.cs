// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine_Residual.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine_Residual
    {
        private static readonly byte[] Nop = [(byte)OpCode.NOP];
        private static readonly byte[] Ret = [(byte)OpCode.RET];
        private static readonly byte[] Push1Ret = [(byte)OpCode.PUSH1, (byte)OpCode.RET];

        private DataCache _snapshot;

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        [TestMethod]
        public void AddFee_Negative_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop);
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.AddFee(-1, false));
        }

        [TestMethod]
        public void AddFee_WithoutFactor_IncreasesFeeConsumed()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop, gas: 100_0000_0000);
            var before = engine.FeeConsumed;
            engine.AddFee(1000, applyFactor: false);
            Assert.IsTrue(engine.FeeConsumed > before);
        }

        [TestMethod]
        public void AddFee_ExceedsLimit_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop, gas: 10);
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.AddFee(1_00000000, applyFactor: true));
        }

        [TestMethod]
        public void Throw_SetsFaultException()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop);
            engine.Throw(new InvalidOperationException("boom"));
            Assert.IsNotNull(engine.FaultException);
            Assert.IsInstanceOfType<InvalidOperationException>(engine.FaultException);
        }

        [TestMethod]
        public void GasLeft_TracksFees()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop, gas: 1_00000000);
            Assert.IsTrue(engine.GasLeft > 0);
            var leftBefore = engine.GasLeft;
            engine.AddFee(1000, false);
            Assert.IsTrue(engine.GasLeft < leftBefore);
            Assert.IsTrue(engine.FeeConsumed > 0);
        }

        [TestMethod]
        public void GetState_SetState_RoundTrip()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop);
            Assert.IsNull(engine.GetState<string>());
            engine.SetState("hello");
            Assert.AreEqual("hello", engine.GetState<string>());

            var bag = new List<int> { 42 };
            engine.SetState(bag);
            Assert.AreSame(bag, engine.GetState<List<int>>());
        }

        [TestMethod]
        public void Notifications_StartsEmpty()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop);
            Assert.IsEmpty(engine.Notifications);
        }

        [TestMethod]
        public void SnapshotCache_IsAvailable()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop);
            Assert.IsNotNull(engine.SnapshotCache);
        }

        [TestMethod]
        public void CurrentScriptHash_MatchesLoadedScript()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Ret);
            Assert.IsNotNull(engine.CurrentScriptHash);
            Assert.IsNotNull(engine.EntryScriptHash);
            Assert.AreEqual(engine.EntryScriptHash, engine.CurrentScriptHash);
        }

        [TestMethod]
        public void IsHardforkEnabled_MatchesProtocolSettings()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop);
            foreach (Hardfork hf in Enum.GetValues<Hardfork>())
            {
                Assert.AreEqual(
                    TestProtocolSettings.Default.IsHardforkEnabled(hf, NativeContract.Ledger.CurrentIndex(_snapshot)),
                    engine.IsHardforkEnabled(hf));
            }
        }

        [TestMethod]
        public void ComposeJumpTables_AreNonNull()
        {
            Assert.IsNotNull(ApplicationEngine.ComposeNotEchidnaJumpTable());
            Assert.IsNotNull(ApplicationEngine.ComposeNotGorgonJumpTable());
        }

        [TestMethod]
        public void Run_ExecutesSimpleScript()
        {
            using var engine = ApplicationEngine.Run(Push1Ret, _snapshot,
                settings: TestProtocolSettings.Default, gas: 100_0000_0000);
            Assert.AreEqual(VMState.HALT, engine.State);
            Assert.AreEqual(1, engine.ResultStack.Count);
            Assert.AreEqual(1, (int)engine.ResultStack.Peek().GetInteger());
        }

        [TestMethod]
        public void LoadContract_NativeSymbol_Succeeds()
        {
            using var engine = TestEngineRunner.Create(_snapshot, gas: 100_0000_0000);
            var contract = NativeContract.ContractManagement.GetContract(_snapshot, NativeContract.NEO.Hash);
            Assert.IsNotNull(contract);
            var method = contract.Manifest.Abi.GetMethod("symbol", 0);
            Assert.IsNotNull(method);
            engine.LoadContract(contract, method, CallFlags.ReadOnly);
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual("NEO", engine.ResultStack.Pop().GetString());
        }

        [TestMethod]
        public void Convert_PrimitiveTypes_ToStackItems()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop);
            Assert.AreEqual(StackItem.Null, engine.Convert(null));
            Assert.IsTrue(engine.Convert(true).GetBoolean());
            Assert.AreEqual(5, (int)engine.Convert((byte)5).GetInteger());
            Assert.AreEqual(7, (int)engine.Convert(7).GetInteger());
            Assert.AreEqual(new BigInteger(9), engine.Convert(9L).GetInteger());
            Assert.AreEqual("hi", engine.Convert("hi").GetString());
            Assert.AreEqual(UInt160.Zero, new UInt160(engine.Convert(UInt160.Zero).GetSpan()));
        }

        [TestMethod]
        public void ValidateCallFlags_MissingFlag_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop);
            engine.LoadScript(new Script(Nop), configureState: s => s.CallFlags = CallFlags.ReadOnly);
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                engine.ValidateCallFlags(CallFlags.WriteStates));
        }

		[TestMethod]
		public void Create_WithPersistingBlock_PreservesBlockAndTrigger()
		{
			var block = new Block
			{
				Header = new Header
				{
					Index = 0,
					Timestamp = 0,
					Nonce = 0,
					NextConsensus = UInt160.Zero,
					PrevHash = UInt256.Zero,
					MerkleRoot = UInt256.Zero,
					Witness = Witness.Empty
				},
				Transactions = []
			};

			using var engine = ApplicationEngine.Create(
				TriggerType.Application,
				null,
				_snapshot,
				persistingBlock: block,
				settings: TestProtocolSettings.Default,
				gas: 100_0000_0000);

			Assert.AreSame(block, engine.PersistingBlock);
			Assert.AreEqual(TriggerType.Application, engine.Trigger);
		}

        [TestMethod]
        public void Services_Dictionary_IsPopulated()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, Nop);
            Assert.IsTrue(ApplicationEngine.Services.Count > 0);
        }
    }
}
