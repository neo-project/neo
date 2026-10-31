// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Syscalls.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System.Linq;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public partial class UT_Syscalls : TestKit
    {
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        public void System_Blockchain_GetBlock()
        {
            var tx = new Transaction()
            {
                Script = new byte[] { 0x01 },
                Attributes = [],
                Signers = [],
                NetworkFee = 0x02,
                SystemFee = 0x03,
                Nonce = 0x04,
                ValidUntilBlock = 0x05,
                Version = 0x06,
                Witnesses = [new() { VerificationScript = new byte[] { 0x07 } }],
            };

            var block = new TrimmedBlock()
            {
                Header = new Header
                {
                    Index = 0,
                    Timestamp = 2,
                    Witness = Witness.Empty,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    PrimaryIndex = 1,
                    NextConsensus = UInt160.Zero,
                },
                Hashes = [tx.Hash]
            };

            var snapshot = _snapshotCache.CloneCache();

            using ScriptBuilder script = new();
            script.EmitDynamicCall(NativeContract.Ledger.Hash, "getBlock", block.Hash.ToArray());

            // Without block

            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);
            Assert.IsTrue(engine.ResultStack.Peek().IsNull);

            // Not traceable block

            const byte Prefix_Transaction = 11;
            const byte Prefix_CurrentBlock = 12;

            TestUtils.BlocksAdd(snapshot, block.Hash, block);

            var height = snapshot[NativeContract.Ledger.CreateStorageKey(Prefix_CurrentBlock)].GetInteroperable<HashIndexState>();
            height.Index = block.Index + TestProtocolSettings.Default.MaxTraceableBlocks;

            snapshot.Add(NativeContract.Ledger.CreateStorageKey(Prefix_Transaction, tx.Hash), new StorageItem(new TransactionState
            {
                BlockIndex = block.Index,
                Transaction = tx
            }));

            engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);
            Assert.IsTrue(engine.ResultStack.Peek().IsNull);

            // With block

            height = snapshot[NativeContract.Ledger.CreateStorageKey(Prefix_CurrentBlock)].GetInteroperable<HashIndexState>();
            height.Index = block.Index;

            engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);

            var array = engine.ResultStack.Pop<VM.Types.Array>();
            Assert.AreEqual(block.Hash, new UInt256(array[0].GetSpan()));
        }

        [TestMethod]
        public void System_ExecutionEngine_GetScriptContainer()
        {
            var snapshot = _snapshotCache.CloneCache();
            using ScriptBuilder script = new();
            script.EmitSysCall(ApplicationEngine.System_Runtime_GetScriptContainer);

            // Without tx

            var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.FAULT, engine.Execute());
            Assert.IsEmpty(engine.ResultStack);

            // With tx

            var tx = new Transaction()
            {
                Script = new byte[] { 0x01 },
                Signers =
                [
                    new()
                    {
                        Account = UInt160.Zero,
                        Scopes = WitnessScope.None,
                        AllowedContracts = [],
                        AllowedGroups = [],
                        Rules = [],
                    }
                ],
                Attributes = [],
                NetworkFee = 0x02,
                SystemFee = 0x03,
                Nonce = 0x04,
                ValidUntilBlock = 0x05,
                Version = 0x06,
                Witnesses = [new() { VerificationScript = new byte[] { 0x07 } }],
            };

            engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshot);
            engine.LoadScript(script.ToArray());

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.HasCount(1, engine.ResultStack);

            var array = engine.ResultStack.Pop<VM.Types.Array>();
            Assert.AreEqual(tx.Hash, new UInt256(array[0].GetSpan()));
        }

        [TestMethod]
        public void System_Runtime_GasLeft()
        {
            var snapshot = _snapshotCache.CloneCache();

            using (var script = new ScriptBuilder())
            {
                script.Emit(OpCode.NOP);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);
                script.Emit(OpCode.NOP);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.Emit(OpCode.NOP);
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);

                // Execute

                var settings = TestProtocolSettings.Default with
                {
                    Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Gorgon, 1).Remove(Hardfork.HF_Huyao)
                };
                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings, gas: 100_000_000);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());

                // Check the results

                Assert.AreSequenceEqual
                    (
                    engine.ResultStack.Select(u => (int)u.GetInteger()).ToArray(),
                    new int[] { 99_999_490, 99_998_980, 99_998_410 }
                    );
            }

            // Check test mode

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Runtime_GasLeft);

                // Execute

                var settings = TestProtocolSettings.Default with
                {
                    Hardforks = TestProtocolSettings.Default.Hardforks.Remove(Hardfork.HF_Huyao)
                };
                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings);
                engine.LoadScript(script.ToArray());

                // Check the results

                Assert.AreEqual(VMState.HALT, engine.Execute());
                Assert.HasCount(1, engine.ResultStack);
                Assert.IsInstanceOfType(engine.ResultStack.Peek(), typeof(Integer));
                Assert.AreEqual(1999999520, engine.ResultStack.Pop().GetInteger());
            }
        }

        [TestMethod]
        public void System_Runtime_GetRandom_VerificationPrice()
        {
            var snapshot = _snapshotCache.CloneCache();

            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_GetRandom);
            script.Emit(OpCode.DROP);

            long GetFee(uint huyaoHeight)
            {
                var settings = TestProtocolSettings.Default with
                {
                    Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, huyaoHeight)
                };
                var engine = ApplicationEngine.Create(TriggerType.Verification, null, snapshot, null, settings);
                engine.LoadScript(script.ToArray(), configureState: p => p.CallFlags = CallFlags.ReadOnly);
                Assert.AreEqual(VMState.HALT, engine.Execute());
                return engine.FeeConsumed;
            }

            // Huyao is configured, but the ledger is still below its height. Verification
            // engine has no persisting block, so the height must be taken from the ledger.
            Assert.IsLessThan(100u, NativeContract.Ledger.CurrentIndex(snapshot));
            // GetRandom in-handler fee (1 << 13) + DROP (1 << 1), multiplied by the default ExecFeeFactor (30) = 245820 datoshi.
            Assert.AreEqual(245820, GetFee(100));

            // GetRandom (15833) + DROP (99 * 1 + 1486), multiplied by 1e-11 GAS * the default ExecFeeFactor (30) = 522.54
            // datoshi since Huyao, rounded up.
            Assert.AreEqual(523, GetFee(0));
        }

        [TestMethod]
        public void System_Runtime_Log_PriceSinceHuyao()
        {
            var snapshot = _snapshotCache.CloneCache();

            using var script = new ScriptBuilder();
            script.EmitPush("a");
            script.EmitSysCall(ApplicationEngine.System_Runtime_Log);

            long GetFee(ProtocolSettings settings)
            {
                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: settings);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());
                return engine.FeeConsumed;
            }

            var feeBefore = GetFee(TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.Remove(Hardfork.HF_Huyao)
            });
            var feeAfter = GetFee(TestProtocolSettings.Default);

            // PUSHDATA1 (1 << 3) + Log (1 << 15), multiplied by ExecFeeFactor = 983280 datoshi before Huyao.
            Assert.AreEqual(983280, feeBefore);
            // PUSHDATA1 (1685) + Log (31767), multiplied by 1e-11 GAS * ExecFeeFactor = 1003.56 datoshi
            // since Huyao, rounded up.
            Assert.AreEqual(1004, feeAfter);
            Assert.IsLessThan(feeBefore, feeAfter);
        }

        [TestMethod]
        public void System_Runtime_GetInvocationCounter()
        {
            var snapshot = _snapshotCache.CloneCache();
            ContractState contractA, contractB, contractC;

            // Create dummy contracts

            using (var script = new ScriptBuilder())
            {
                script.EmitSysCall(ApplicationEngine.System_Runtime_GetInvocationCounter);

                contractA = TestUtils.GetContract(new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP }.Concat(script.ToArray()).ToArray());
                contractB = TestUtils.GetContract(new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray());
                contractC = TestUtils.GetContract(new byte[] { (byte)OpCode.DROP, (byte)OpCode.DROP, (byte)OpCode.NOP, (byte)OpCode.NOP }.Concat(script.ToArray()).ToArray());
                contractA.Hash = contractA.Script.Span.ToScriptHash();
                contractB.Hash = contractB.Script.Span.ToScriptHash();
                contractC.Hash = contractC.Script.Span.ToScriptHash();

                // Init A,B,C contracts
                // First two drops is for drop method and arguments

                snapshot.DeleteContract(contractA.Hash);
                snapshot.DeleteContract(contractB.Hash);
                snapshot.DeleteContract(contractC.Hash);
                contractA.Manifest = TestUtils.CreateManifest("dummyMain", ContractParameterType.Any, ContractParameterType.String, ContractParameterType.Integer);
                contractB.Manifest = TestUtils.CreateManifest("dummyMain", ContractParameterType.Any, ContractParameterType.String, ContractParameterType.Integer);
                contractC.Manifest = TestUtils.CreateManifest("dummyMain", ContractParameterType.Any, ContractParameterType.String, ContractParameterType.Integer);
                snapshot.AddContract(contractA.Hash, contractA);
                snapshot.AddContract(contractB.Hash, contractB);
                snapshot.AddContract(contractC.Hash, contractC);
            }

            // Call A,B,B,C

            using (var script = new ScriptBuilder())
            {
                script.EmitDynamicCall(contractA.Hash, "dummyMain", "0", 1);
                script.EmitDynamicCall(contractB.Hash, "dummyMain", "0", 1);
                script.EmitDynamicCall(contractB.Hash, "dummyMain", "0", 1);
                script.EmitDynamicCall(contractC.Hash, "dummyMain", "0", 1);

                // Execute

                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, ProtocolSettings.Default);
                engine.LoadScript(script.ToArray());
                Assert.AreEqual(VMState.HALT, engine.Execute());

                // Check the results

                Assert.AreSequenceEqual(
                    engine.ResultStack.Select(u => (int)u.GetInteger()).ToArray(),
                    new int[] { 1 /* A */, 1 /* B */, 2 /* B */, 1  /* C */});
            }
        }
    }
}
