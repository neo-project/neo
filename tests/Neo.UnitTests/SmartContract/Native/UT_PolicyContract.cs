// Copyright (C) 2015-2026 The Neo Project.
//
// UT_PolicyContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Iterators;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_PolicyContract
    {
        private DataCache _snapshotCache;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void Transfer_RoutesGasAfterBlockingDuringPayment(bool destroy, bool huyao)
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var callback = new ScriptBuilder();
            callback.Emit(OpCode.CLEAR);
            if (destroy)
                callback.EmitDynamicCall(NativeContract.ContractManagement.Hash, "destroy");
            else
            {
                callback.EmitSysCall(ApplicationEngine.System_Runtime_GetExecutingScriptHash);
                callback.EmitPush(1);
                callback.Emit(OpCode.PACK);
                callback.EmitPush((int)CallFlags.All);
                callback.EmitPush("blockAccount");
                callback.EmitPush(NativeContract.Policy.Hash);
                callback.EmitSysCall(ApplicationEngine.System_Contract_Call);
            }
            callback.Emit(OpCode.DROP);
            callback.Emit(OpCode.RET);
            var manifest = TestUtils.CreateManifest("onNEP17Payment", ContractParameterType.Void,
                ContractParameterType.Hash160, ContractParameterType.Integer, ContractParameterType.Any);
            var contract = TestUtils.GetContract(callback.ToArray(), manifest);
            snapshot.AddContract(contract.Hash, contract);
            var sender = UInt160.Parse("0x0000000000000000000000000000000000001234");
            snapshot.Add(NativeContract.NEO.CreateStorageKey(20, contract.Hash),
                new StorageItem(new NeoToken.NeoAccountState { Balance = 100, BalanceHeight = 0 }));
            snapshot.Add(NativeContract.NEO.CreateStorageKey(20, sender),
                new StorageItem(new NeoToken.NeoAccountState { Balance = 10, BalanceHeight = 1000 }));
            var block = new Block
            {
                Header = new Header
                {
                    Index = 1000,
                    Timestamp = 1_000_000,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, huyao ? 1000u : 1001u)
            };
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(sender, NativeContract.NEO.GetCommitteeAddress(snapshot)),
                snapshot, block, settings);
            var expected = NativeContract.NEO.UnclaimedGas(engine, contract.Hash, 1000);
            var before = NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash);
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "transfer", sender, contract.Hash, 1, null);
            engine.LoadScript(script.ToArray());
            if (!huyao && !destroy)
            {
                Assert.AreEqual(VMState.FAULT, engine.Execute());
                Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, contract.Hash));
                return;
            }
            Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, contract.Hash));
            Assert.AreEqual(before + (huyao ? expected : BigInteger.Zero), NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash),
                $"GAS at blocked account: {NativeContract.GAS.BalanceOf(snapshot, contract.Hash)}");
            Assert.AreEqual(huyao ? BigInteger.Zero : expected, NativeContract.GAS.BalanceOf(snapshot, contract.Hash));
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public void Transfer_SettlesBothRewardsOnceWhenOneAccountIsBlocked(bool blockSender, bool huyao)
        {
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, huyao ? 0u : 1001u).Remove(Hardfork.HF_Iara).Remove(Hardfork.HF_Jackalope)
            };
            var system = new TestBlockchain.TestNeoSystem(settings);
            var snapshot = system.GetSnapshotCache().CloneCache();
            var sender = UInt160.Parse("0x0000000000000000000000000000000000001234");
            using var callback = new ScriptBuilder();
            callback.Emit(OpCode.CLEAR);
            if (blockSender)
                callback.EmitDynamicCall(NativeContract.Policy.Hash, "blockAccount", sender);
            else
            {
                callback.EmitSysCall(ApplicationEngine.System_Runtime_GetExecutingScriptHash);
                callback.EmitPush(1);
                callback.Emit(OpCode.PACK);
                callback.EmitPush((int)CallFlags.All);
                callback.EmitPush("blockAccount");
                callback.EmitPush(NativeContract.Policy.Hash);
                callback.EmitSysCall(ApplicationEngine.System_Contract_Call);
            }
            callback.Emit(OpCode.DROP);
            callback.Emit(OpCode.RET);
            var manifest = TestUtils.CreateManifest("onNEP17Payment", ContractParameterType.Void,
                ContractParameterType.Hash160, ContractParameterType.Integer, ContractParameterType.Any);
            var contract = TestUtils.GetContract(callback.ToArray(), manifest);
            snapshot.AddContract(contract.Hash, contract);
            snapshot.Add(NativeContract.NEO.CreateStorageKey(20, contract.Hash),
                new StorageItem(new NeoToken.NeoAccountState { Balance = 100, BalanceHeight = 0 }));
            snapshot.Add(NativeContract.NEO.CreateStorageKey(20, sender),
                new StorageItem(new NeoToken.NeoAccountState { Balance = 10, BalanceHeight = 0 }));
            var block = new Block
            {
                Header = new Header
                {
                    Index = 1000,
                    Timestamp = 1_000_000,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(sender, NativeContract.NEO.GetCommitteeAddress(snapshot)),
                snapshot, block, settings);
            var senderReward = NativeContract.NEO.UnclaimedGas(engine, sender, 1000);
            var receiverReward = NativeContract.NEO.UnclaimedGas(engine, contract.Hash, 1000);
            Assert.IsGreaterThan(BigInteger.Zero, senderReward);
            Assert.IsGreaterThan(senderReward, receiverReward);
            var treasuryBefore = NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash);
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NEO.Hash, "transfer", sender, contract.Hash, 1, null);
            engine.LoadScript(script.ToArray());

            // Before Huyao, minting to a contract blocked during the callback still faults.
            if (!huyao && !blockSender)
            {
                Assert.AreEqual(VMState.FAULT, engine.Execute());
                Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, contract.Hash));
                Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, sender));
                Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, contract.Hash));
                return;
            }

            Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            var blocked = blockSender ? sender : contract.Hash;
            var blockedReward = blockSender ? senderReward : receiverReward;
            var unblockedReward = blockSender ? receiverReward : senderReward;
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, blocked));
            Assert.AreEqual(!blockSender, NativeContract.Policy.IsBlocked(snapshot, contract.Hash));
            Assert.AreEqual(blockSender && huyao ? BigInteger.Zero : senderReward, NativeContract.GAS.BalanceOf(snapshot, sender));
            Assert.AreEqual(!blockSender && huyao ? BigInteger.Zero : receiverReward, NativeContract.GAS.BalanceOf(snapshot, contract.Hash));
            Assert.AreEqual(treasuryBefore + (huyao ? blockedReward : BigInteger.Zero), NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash));
            Assert.AreEqual(senderReward + receiverReward,
                NativeContract.GAS.BalanceOf(snapshot, sender) + NativeContract.GAS.BalanceOf(snapshot, contract.Hash)
                + NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash) - treasuryBefore);
            Assert.AreEqual(unblockedReward, blockSender
                ? NativeContract.GAS.BalanceOf(snapshot, contract.Hash)
                : NativeContract.GAS.BalanceOf(snapshot, sender));
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(engine, sender, 1000));
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(engine, contract.Hash, 1000));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void BlockAccount_GasClaimDependsOnHuyao(bool activated)
        {
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, activated ? 0u : 1001u).Remove(Hardfork.HF_Iara).Remove(Hardfork.HF_Jackalope)
            };
            var system = new TestBlockchain.TestNeoSystem(settings);
            var snapshot = system.GetSnapshotCache().CloneCache();
            var account = UInt160.Parse("0x0000000000000000000000000000000000001234");
            var key = NativeContract.NEO.CreateStorageKey(20, account);
            snapshot.Add(key, new StorageItem(new NeoToken.NeoAccountState { Balance = 100, BalanceHeight = 0 }));
            var block = new Block
            {
                Header = new Header
                {
                    Index = 1000,
                    Timestamp = 1_000_000,
                    Witness = Witness.Empty,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero
                },
                Transactions = []
            };
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(NativeContract.NEO.GetCommitteeAddress(snapshot)),
                snapshot, block, settings);
            var result = NativeContract.Policy.Call(engine, "blockAccount",
                new ContractParameter(ContractParameterType.Hash160) { Value = account });
            Assert.IsTrue(result.GetBoolean());
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, account));
            Assert.AreEqual(activated, NativeContract.GAS.BalanceOf(snapshot, account).IsZero);
            var state = snapshot.TryGet(key)!.GetInteroperable<NeoToken.NeoAccountState>();
            Assert.AreEqual(1000u, state.BalanceHeight);
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(engine, account, block.Index));
            Assert.AreEqual(activated, NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash) > 0);
        }

        [TestMethod]
        public void BlockAccount_MintsGasToTreasuryAndPreventsRevoting()
        {
            var snapshot = _snapshotCache.CloneCache();
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var committees = NativeContract.NEO.GetCommittee(snapshot);
            var candidatePubKey = committees[0];

            ulong startTime = 1_000_000;

            Block blockStart = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    Timestamp = startTime,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };

            var candidateKey = NativeContract.NEO.CreateStorageKey(33, candidatePubKey);
            snapshot.GetAndChange(candidateKey, () => new StorageItem(new NeoToken.CandidateState { Registered = true, Votes = 0 }));

            // Blocking must succeed without executing the aborting callback, minting GAS to Treasury.
            var abortScript = new byte[] { (byte)OpCode.ABORT };
            var abortManifest = TestUtils.CreateManifest("onNEP17Payment", ContractParameterType.Void,
                ContractParameterType.Hash160, ContractParameterType.Integer, ContractParameterType.Any);
            var abortContract = TestUtils.GetContract(abortScript, abortManifest);
            snapshot.DeleteContract(abortContract.Hash);
            snapshot.AddContract(abortContract.Hash, abortContract);

            var abortNeoKey = NativeContract.NEO.CreateStorageKey(20, abortContract.Hash);
            snapshot.GetAndChange(abortNeoKey, () => new StorageItem(new NeoToken.NeoAccountState
            {
                Balance = 1,
                BalanceHeight = 0
            }));

            var abortRet = NativeContract.Policy.Call(
                    snapshot,
                    new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                    blockStart,
                    "blockAccount",
                    new ContractParameter(ContractParameterType.Hash160) { Value = abortContract.Hash });
            Assert.IsTrue(abortRet.GetBoolean());
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, abortContract.Hash));
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, abortContract.Hash));
            Assert.AreEqual(1, NativeContract.NEO.BalanceOf(snapshot, abortContract.Hash));

            // The callback must not be able to vote again during blocking.
            byte[] revoteScript;
            using (var sb = new ScriptBuilder())
            {
                sb.Emit(OpCode.CLEAR);
                sb.EmitPush(candidatePubKey.EncodePoint(true));
                sb.EmitSysCall(ApplicationEngine.System_Runtime_GetExecutingScriptHash);
                sb.EmitPush(2);
                sb.Emit(OpCode.PACK);
                sb.EmitPush((int)CallFlags.All);
                sb.EmitPush("vote");
                sb.EmitPush(NativeContract.NEO.Hash);
                sb.EmitSysCall(ApplicationEngine.System_Contract_Call);
                sb.Emit(OpCode.DROP);
                sb.Emit(OpCode.RET);
                revoteScript = sb.ToArray();
            }
            var revoteManifest = TestUtils.CreateManifest("onNEP17Payment", ContractParameterType.Void,
                ContractParameterType.Hash160, ContractParameterType.Integer, ContractParameterType.Any);
            var revoteContract = TestUtils.GetContract(revoteScript, revoteManifest);
            snapshot.DeleteContract(revoteContract.Hash);
            snapshot.AddContract(revoteContract.Hash, revoteContract);

            var revoteNeoKey = NativeContract.NEO.CreateStorageKey(20, revoteContract.Hash);
            snapshot.GetAndChange(revoteNeoKey, () => new StorageItem(new NeoToken.NeoAccountState
            {
                Balance = 100,
                BalanceHeight = 0
            }));

            var blockRet = NativeContract.Policy.Call(
                snapshot,
                new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                blockStart,
                "blockAccount",
                new ContractParameter(ContractParameterType.Hash160) { Value = revoteContract.Hash });
            Assert.IsTrue(blockRet.GetBoolean());
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, revoteContract.Hash));

            var revoteState = snapshot.TryGet(revoteNeoKey)!.GetInteroperable<NeoToken.NeoAccountState>();
            Assert.IsNull(revoteState.VoteTo);
            var candidateVotesAfterBlock = snapshot.TryGet(candidateKey)!.GetInteroperable<NeoToken.CandidateState>().Votes;
            Assert.AreEqual(0, candidateVotesAfterBlock);
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, revoteContract.Hash));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RecoverFund_NeoWithAccruedGasRoutesToTreasuryFromHuyao(bool activated)
        {
            var snapshot = _snapshotCache.CloneCache();
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var committees = NativeContract.NEO.GetCommittee(snapshot);
            var min = Math.Max(1, committees.Length - (committees.Length - 1) / 2);
            var committeeFullMultiSigAddr = Contract.CreateMultiSigRedeemScript(Math.Max(min, committees.Length - 2), committees).ToScriptHash();
            const ulong startTime = 1_000_000;
            const ulong requiredTime = 365UL * 24 * 60 * 60 * 1_000;
            Block blockStart = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    Timestamp = startTime,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };
            Block blockFinish = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 2000,
                    Timestamp = startTime + requiredTime + 1_000,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };

            // Huyao routes the blocked account reward to Treasury during recovery.
            var benignScript = new byte[] { (byte)OpCode.CLEAR, (byte)OpCode.RET };
            var benignManifest = TestUtils.CreateManifest("onNEP17Payment", ContractParameterType.Void,
                ContractParameterType.Hash160, ContractParameterType.Integer, ContractParameterType.Any);
            var benignContract = TestUtils.GetContract(benignScript, benignManifest);
            snapshot.DeleteContract(benignContract.Hash);
            snapshot.AddContract(benignContract.Hash, benignContract);

            var benignNeoKey = NativeContract.NEO.CreateStorageKey(20, benignContract.Hash);
            snapshot.GetAndChange(benignNeoKey, () => new StorageItem(new NeoToken.NeoAccountState
            {
                Balance = 50,
                BalanceHeight = 0
            }));

            var benignBlockRet = NativeContract.Policy.Call(
                snapshot,
                new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                blockStart,
                "blockAccount",
                new ContractParameter(ContractParameterType.Hash160) { Value = benignContract.Hash });
            Assert.IsTrue(benignBlockRet.GetBoolean());
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, benignContract.Hash));

            var treasuryBefore = NativeContract.NEO.BalanceOf(snapshot, NativeContract.Treasury.Hash);
            var gasBefore = NativeContract.GAS.BalanceOf(snapshot, benignContract.Hash);
            var treasuryGasBefore = NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash);
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, activated ? 2000u : 2001u)
            };
            // The stored native ABI must match the simulated hardfork schedule.
            foreach (var native in NativeContract.Contracts)
            {
                snapshot.DeleteContract(native.Hash);
                snapshot.AddContract(native.Hash, native.GetContractState(settings, snapshot, blockFinish.Index));
            }
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(committeeFullMultiSigAddr), snapshot, blockFinish, settings);
            void Recover() => NativeContract.Policy.Call(engine,
                    "recoverFund",
                    new ContractParameter(ContractParameterType.Hash160) { Value = benignContract.Hash },
                    new ContractParameter(ContractParameterType.Hash160) { Value = NativeContract.NEO.Hash });
            var pending = NativeContract.NEO.UnclaimedGas(engine, benignContract.Hash, blockFinish.Index);
            Assert.IsGreaterThan(BigInteger.Zero, pending);
            if (activated)
            {
                Recover();
                Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.BalanceOf(snapshot, benignContract.Hash));
                Assert.AreEqual(treasuryBefore + 50, NativeContract.NEO.BalanceOf(snapshot, NativeContract.Treasury.Hash));
                Assert.AreEqual(treasuryGasBefore + pending, NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash));
            }
            else
            {
                var ex = Assert.ThrowsExactly<InvalidOperationException>(Recover);
                Assert.Contains("has been blocked", ex.Message);
                Assert.AreEqual(50, NativeContract.NEO.BalanceOf(snapshot, benignContract.Hash));
                Assert.AreEqual(treasuryBefore, NativeContract.NEO.BalanceOf(snapshot, NativeContract.Treasury.Hash));
                using var query = ApplicationEngine.Create(TriggerType.Application, null, snapshot, blockFinish, settings);
                Assert.AreEqual(pending, NativeContract.NEO.UnclaimedGas(query, benignContract.Hash, blockFinish.Index));
            }
            Assert.AreEqual(gasBefore, NativeContract.GAS.BalanceOf(snapshot, benignContract.Hash));
        }

        [TestMethod]
        public void BlockedAccount_ReceivingNeoMintsGasToTreasuryAndSettlesRewardHeight()
        {
            var snapshot = _snapshotCache.CloneCache();
            var account = UInt160.Parse("0x0000000000000000000000000000000000001234");
            var sender = UInt160.Parse("0x0000000000000000000000000000000000005678");
            var accountKey = NativeContract.NEO.CreateStorageKey(20, account);
            snapshot.Add(accountKey, new StorageItem(new NeoToken.NeoAccountState { Balance = 50, BalanceHeight = 0 }));
            snapshot.Add(NativeContract.NEO.CreateStorageKey(20, sender),
                new StorageItem(new NeoToken.NeoAccountState { Balance = 50, BalanceHeight = 0 }));
            var committee = new Nep17NativeContractExtensions.ManualWitness(NativeContract.NEO.GetCommitteeAddress(snapshot));
            var block = new Block
            {
                Header = new Header
                {
                    Index = 1000,
                    Timestamp = 1_000_000,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };
            NativeContract.Policy.Call(snapshot, committee, block, "blockAccount",
                new ContractParameter(ContractParameterType.Hash160) { Value = account });
            using var query = ApplicationEngine.Create(TriggerType.Application, null, snapshot, block, TestProtocolSettings.Default);
            var treasuryBefore = NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash);
            Assert.IsGreaterThan(BigInteger.Zero, treasuryBefore);
            block.Header.Index = 2000;
            var pending = NativeContract.NEO.UnclaimedGas(query, account, block.Index);
            Assert.IsGreaterThan(BigInteger.Zero, pending);
            var result = NativeContract.NEO.Call(snapshot,
                new Nep17NativeContractExtensions.ManualWitness(sender), block, "transfer",
                new ContractParameter(ContractParameterType.Hash160) { Value = sender },
                new ContractParameter(ContractParameterType.Hash160) { Value = account },
                new ContractParameter(ContractParameterType.Integer) { Value = 50 },
                new ContractParameter(ContractParameterType.Any));
            Assert.IsTrue(result.GetBoolean());
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, account));
            Assert.AreEqual(treasuryBefore + pending, NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash));
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(query, account, block.Index));
            Assert.IsGreaterThan(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, sender));
            var state = snapshot.TryGet(accountKey)!.GetInteroperable<NeoToken.NeoAccountState>();
            Assert.AreEqual(new BigInteger(100), state.Balance);
            Assert.AreEqual(2000u, state.BalanceHeight);

            NativeContract.Policy.Call(snapshot, committee, block, "unblockAccount",
                new ContractParameter(ContractParameterType.Hash160) { Value = account });
            block.Header.Index = 2001;
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(account), snapshot, block, TestProtocolSettings.Default);
            var expected = NativeContract.NEO.UnclaimedGas(engine, account, block.Index);
            // The transfer already claimed the old rewards. Only the newly accrued
            // reward for 100 NEO over one block remains; no retroactive rewards.
            Assert.AreEqual(expected, pending / 500);
            result = NativeContract.NEO.Call(engine, "transfer",
                new ContractParameter(ContractParameterType.Hash160) { Value = account },
                new ContractParameter(ContractParameterType.Hash160) { Value = account },
                new ContractParameter(ContractParameterType.Integer) { Value = 0 },
                new ContractParameter(ContractParameterType.Any));
            Assert.IsTrue(result.GetBoolean());
            Assert.AreEqual(expected, NativeContract.GAS.BalanceOf(snapshot, account));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void BlockAccount_MintsHolderAndVotingRewardsToTreasury(bool emptyBalance)
        {
            var snapshot = _snapshotCache.CloneCache();
            var account = UInt160.Parse("0x0000000000000000000000000000000000001234");
            var receiver = UInt160.Parse("0x0000000000000000000000000000000000005678");
            var candidate = NativeContract.NEO.GetCommittee(snapshot)[0];
            var accountKey = NativeContract.NEO.CreateStorageKey(20, account);
            snapshot.Add(accountKey, new StorageItem(new NeoToken.NeoAccountState
            {
                Balance = 100,
                BalanceHeight = 0,
                VoteTo = candidate,
                LastGasPerVote = 100_00000000L
            }));
            snapshot.GetAndChange(NativeContract.NEO.CreateStorageKey(33, candidate),
                () => new StorageItem(new NeoToken.CandidateState())).FromReplica(
                    new StorageItem(new NeoToken.CandidateState { Registered = true, Votes = 100 }));
            snapshot.GetAndChange(NativeContract.NEO.CreateStorageKey(1)).Add(100);
            snapshot.GetAndChange(NativeContract.NEO.CreateStorageKey(23, candidate),
                () => new StorageItem(BigInteger.Zero)).Set(new BigInteger(103_00000000L));
            var block = new Block
            {
                Header = new Header
                {
                    Index = 1000,
                    Timestamp = 1_000_000,
                    Witness = Witness.Empty,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero
                },
                Transactions = []
            };
            var committee = new Nep17NativeContractExtensions.ManualWitness(NativeContract.NEO.GetCommitteeAddress(snapshot));
            using var query = ApplicationEngine.Create(TriggerType.Application, null, snapshot, block, TestProtocolSettings.Default);
            var expected = NativeContract.NEO.UnclaimedGas(query, account, block.Index);
            Assert.IsGreaterThan(new BigInteger(300), expected);
            NativeContract.Policy.Call(snapshot, committee, block, "blockAccount",
                new ContractParameter(ContractParameterType.Hash160) { Value = account });
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, account));
            Assert.IsNull(snapshot.TryGet(accountKey).GetInteroperable<NeoToken.NeoAccountState>().VoteTo);
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(query, account, block.Index));
            // Clearing the vote preserves the old reward, without earning later
            // increases in the candidate's reward accumulator.
            snapshot.GetAndChange(NativeContract.NEO.CreateStorageKey(23, candidate)).Add(100_00000000L);
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(query, account, block.Index));

            // Rewards have already been minted to Treasury when blocking.
            // Repeated transfers must not duplicate holder or voting rewards.
            Assert.IsTrue(NativeContract.NEO.Transfer(snapshot, account.ToArray(), receiver.ToArray(),
                emptyBalance ? 100 : 0, true, block));
            Assert.IsTrue(NativeContract.NEO.Transfer(snapshot, account.ToArray(), receiver.ToArray(), 0, true, block));
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, account));
            Assert.AreEqual(expected, NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash));
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(query, account, block.Index));
            if (emptyBalance) Assert.IsNull(snapshot.TryGet(accountKey));

            NativeContract.Policy.Call(snapshot, committee, block, "unblockAccount",
                new ContractParameter(ContractParameterType.Hash160) { Value = account });
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, account));
            Assert.AreEqual(expected, NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash));
            Assert.IsTrue(NativeContract.NEO.Transfer(snapshot, account.ToArray(), receiver.ToArray(), 0, true, block));
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, account));
            Assert.AreEqual(expected, NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash));
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(query, account, block.Index));
            Assert.IsTrue(NativeContract.NEO.Transfer(snapshot, account.ToArray(), receiver.ToArray(), 0, true, block));
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, account));
            Assert.AreEqual(expected, NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash));
        }

        [TestMethod]
        public void Check_Default()
        {
            var snapshot = _snapshotCache.CloneCache();

            var ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(1000, ret.GetInteger());

            ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.Conflicts });
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(PolicyContract.DefaultAttributeFee, ret.GetInteger());

            Assert.ThrowsExactly<InvalidOperationException>(() => _ = NativeContract.Policy.Call(snapshot, "getAttributeFee", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)byte.MaxValue }));
        }

        [TestMethod]
        public void Check_SetAttributeFee()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain
            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            var attr = new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)TransactionAttributeType.Conflicts };

            // Without signature
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(0, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 11_0000_0000 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(0, ret.GetInteger());

            // Proper set
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 300300 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(300300, ret.GetInteger());

            // Set to zero
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setAttributeFee", attr, new ContractParameter(ContractParameterType.Integer) { Value = 0 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getAttributeFee", attr);
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(0, ret.GetInteger());
        }

        [TestMethod]
        public void Check_SetFeePerByte()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            // Without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(1000, ret.GetInteger());

            // With signature
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setFeePerByte", new ContractParameter(ContractParameterType.Integer) { Value = 1 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getFeePerByte");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(1, ret.GetInteger());
        }

        [TestMethod]
        public void Check_GetExecFeeFactorWithDecimals()
        {
            var snapshot = _snapshotCache.CloneCache();
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Faun, 10)
            };
            snapshot.GetAndChange(StorageKey.Create(NativeContract.Policy.Id, 18))!.Set(305001);

            // Since Faun the factor is stored with the decimals.
            Assert.AreEqual(305001, NativeContract.Policy.GetExecFeeFactorWithDecimals(settings, snapshot, 10));
            Assert.AreEqual(30, NativeContract.Policy.GetExecFeeFactor(settings, snapshot, 10));
            // Before Faun the factor is stored without the decimals.
            Assert.AreEqual(305001 * ApplicationEngine.FeeFactor, NativeContract.Policy.GetExecFeeFactorWithDecimals(settings, snapshot, 9));
            Assert.AreEqual(305001, NativeContract.Policy.GetExecFeeFactor(settings, snapshot, 9));
        }

        [TestMethod]
        public void Check_SetBaseExecFee()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            // Without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setExecFeeFactor", new ContractParameter(ContractParameterType.Integer) { Value = 50 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getExecFeeFactor");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(30, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setExecFeeFactor", new ContractParameter(ContractParameterType.Integer) { Value = 100500_0000 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getExecFeeFactor");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(30, ret.GetInteger());

            // Proper set
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setExecFeeFactor", new ContractParameter(ContractParameterType.Integer) { Value = 50_0000 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getExecFeeFactor");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(50, ret.GetInteger());
        }

        [TestMethod]
        public void Check_RecoverFunds_CompleteFlow()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Get almost full committee address
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var committees = NativeContract.NEO.GetCommittee(snapshot);
            var min = Math.Max(1, committees.Length - (committees.Length - 1) / 2);
            var committeeFullMultiSigAddr = Contract.CreateMultiSigRedeemScript(Math.Max(min, committees.Length - 2), committees).ToScriptHash();
            // Create a blocked account
            UInt160 blockedAccount = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            ulong startTime = 1000000;
            ulong requiredTime = 365UL * 24 * 60 * 60 * 1_000; // Actual value from code
            ulong finishTime = startTime + requiredTime + 1000; // More than required time

            // Block 1: For recoverFundsStart
            Block blockStart = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    Timestamp = startTime,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            // Block 2: For recoverFundsFinish (more than 1 year later)
            Block blockFinish = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 2000,
                    Timestamp = finishTime,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            // Try Without signature
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), blockStart,
                    "recoverFund",
                    new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero },
                    new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            });
            // Step 1: Block the account
            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), blockStart,
                "blockAccount",
                new ContractParameter(ContractParameterType.Hash160) { Value = blockedAccount });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsTrue(ret.GetBoolean());
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, blockedAccount));

            // Step 2: Set account balances (GAS)
            var gasBalance = 50000 * NativeContract.GAS.Factor; // 50000 GAS

            // Set GAS balance
            var gasKey = NativeContract.GAS.CreateStorageKey(20, blockedAccount);
            var gasEntry = snapshot.GetAndChange(gasKey, () => new StorageItem(new AccountState()));
            gasEntry.GetInteroperable<AccountState>().Balance = gasBalance;

            // Verify balances are set
            Assert.AreEqual(gasBalance, NativeContract.GAS.BalanceOf(snapshot, blockedAccount));

            // Step 3: Call recoverFundsFinish (after required time has passed)
            // This should transfer all funds to Treasury
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeFullMultiSigAddr), blockFinish,
                "recoverFund",
                new ContractParameter(ContractParameterType.Hash160) { Value = blockedAccount },
                new ContractParameter(ContractParameterType.Hash160) { Value = NativeContract.GAS.Hash });

            // Step 5: Verify balances were transferred to Treasury
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, blockedAccount));

            // Verify Treasury received the funds
            var treasuryGasBalance = NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash);
            // Treasury should have received the funds (exact balance depends on initial Treasury balance)
            Assert.IsTrue(treasuryGasBalance >= gasBalance, "Treasury should have received GAS");
        }

        [TestMethod]
        public void Check_SetStoragePrice()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            // Without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setStoragePrice", new ContractParameter(ContractParameterType.Integer) { Value = 100500 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getStoragePrice");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(100000, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setStoragePrice", new ContractParameter(ContractParameterType.Integer) { Value = 100000000 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getStoragePrice");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(100000, ret.GetInteger());

            // Proper set
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setStoragePrice", new ContractParameter(ContractParameterType.Integer) { Value = 300300 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getStoragePrice");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(300300, ret.GetInteger());
        }

        [TestMethod]
        public void Check_SetMaxValidUntilBlockIncrement()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain
            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            // Without signature
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setMaxValidUntilBlockIncrement", new ContractParameter(ContractParameterType.Integer) { Value = 123 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getMaxValidUntilBlockIncrement");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(5760, ret.GetInteger());

            // With signature, wrong value
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxValidUntilBlockIncrement", new ContractParameter(ContractParameterType.Integer) { Value = 100000000 });
            });

            ret = NativeContract.Policy.Call(snapshot, "getMaxValidUntilBlockIncrement");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(5760, ret.GetInteger());

            // Proper set
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setMaxValidUntilBlockIncrement", new ContractParameter(ContractParameterType.Integer) { Value = 123 });
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getMaxValidUntilBlockIncrement");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(123, ret.GetInteger());

            // Update MaxTraceableBlocks value for further test.
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 6000 });
            Assert.IsTrue(ret.IsNull);

            // Set MaxValudUntilBlockIncrement to be larger or equal to MaxTraceableBlocks, it should fail.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxValidUntilBlockIncrement", new ContractParameter(ContractParameterType.Integer) { Value = 6000 });
            });
        }

        [TestMethod]
        public void Check_SetMillisecondsPerBlock()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain.
            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            // Without signature.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setMillisecondsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 123 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getMillisecondsPerBlock");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(15_000, ret.GetInteger());

            // With signature, too big value.
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMillisecondsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 30_001 });
            });

            // With signature, too small value.
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMillisecondsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 0 });
            });

            // Ensure value is not changed.
            ret = NativeContract.Policy.Call(snapshot, "getMillisecondsPerBlock");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(15_000, ret.GetInteger());

            // Proper set.
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setMillisecondsPerBlock", new ContractParameter(ContractParameterType.Integer) { Value = 3_000 });
            Assert.IsTrue(ret.IsNull);

            // Ensure value is updated.
            ret = NativeContract.Policy.Call(snapshot, "getMillisecondsPerBlock");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(3_000, ret.GetInteger());
        }

        [TestMethod]
        public void Check_BlockAccount()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            // Without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), block,
                "blockAccount",
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            });

            // With signature

            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
              "blockAccount",
              new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsTrue(ret.GetBoolean());

            // Same account
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount",
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsFalse(ret.GetBoolean());

            // Account B

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount",
                new ContractParameter(ContractParameterType.ByteArray) { Value = UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01").ToArray() });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsTrue(ret.GetBoolean());

            // Check

            Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01")));
            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Parse("0xb400ff00ff00ff00ff00ff00ff00ff00ff00ff01")));
        }

        [TestMethod]
        public void Check_Block_UnblockAccount()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // Block without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            });

            Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

            // Block with signature

            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsTrue(ret.GetBoolean());

            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

            // Unblock without signature

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            });

            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

            // Unblock with signature

            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "unblockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            Assert.IsInstanceOfType(ret, typeof(Boolean));
            Assert.IsTrue(ret.GetBoolean());

            Assert.IsFalse(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));
        }

        [TestMethod]
        public void Check_SetMaxTraceableBlocks()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain.
            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            // Without signature.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 123 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getMaxTraceableBlocks");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(2_102_400, ret.GetInteger());

            // With signature, too big value.
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 2_102_401 });
            });

            // With signature, too small value.
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 0 });
            });

            // With signature, lower or equal to MaxValidUntilBlockIncrement.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 5760 });
            });

            // Ensure value is not changed.
            ret = NativeContract.Policy.Call(snapshot, "getMaxTraceableBlocks");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(2102400, ret.GetInteger());

            // Proper set.
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 5761 });
            Assert.IsTrue(ret.IsNull);

            // Ensure value is updated.
            ret = NativeContract.Policy.Call(snapshot, "getMaxTraceableBlocks");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(5761, ret.GetInteger());

            // Larger value should be prohibited.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "setMaxTraceableBlocks", new ContractParameter(ContractParameterType.Integer) { Value = 5762 });
            });
        }

        [TestMethod]
        public void TestListBlockedAccounts()
        {
            var snapshot = _snapshotCache.CloneCache();

            // Fake blockchain

            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            var ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            Assert.IsInstanceOfType<Boolean>(ret);
            Assert.IsTrue(ret.GetBoolean());

            Assert.IsTrue(NativeContract.Policy.IsBlocked(snapshot, UInt160.Zero));

            var sb = new ScriptBuilder()
                .EmitDynamicCall(NativeContract.Policy.Hash, "getBlockedAccounts");

            var engine = ApplicationEngine.Run(sb.ToArray(), snapshot, null, block, TestBlockchain.GetSystem().Settings);

            Assert.IsInstanceOfType<InteropInterface>(engine.ResultStack[0]);

            var iter = engine.ResultStack[0].GetInterface<object>() as StorageIterator;
            Assert.IsTrue(iter.Next());
            Assert.AreEqual(new UInt160(iter.Value(out _).GetSpan()), UInt160.Zero);
        }

        [TestMethod]
        public void TestWhiteListFee()
        {
            // Create script

            var snapshotCache = _snapshotCache.CloneCache();

            byte[] script;
            using (var sb = new ScriptBuilder())
            {
                sb.EmitDynamicCall(NativeContract.NEO.Hash, "balanceOf", NativeContract.NEO.GetCommitteeAddress(_snapshotCache.CloneCache()));
                script = sb.ToArray();
            }

            var engine = CreateEngineWithCommitteeSigner(snapshotCache, script);

            // Not whitelisted

            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Pop().GetInteger());
            Assert.AreEqual(2028330, engine.FeeConsumed);
            Assert.AreEqual(0, NativeContract.Policy.CleanWhitelist(engine, NativeContract.NEO.GetContractState(engine.ProtocolSettings, engine.SnapshotCache, 0)));
            Assert.IsEmpty(engine.Notifications);

            // Whitelist

            engine = CreateEngineWithCommitteeSigner(snapshotCache, script);

            NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, "balanceOf", 1, 0);
            engine.SnapshotCache.Commit();

            // Whitelisted

            Assert.HasCount(1, engine.Notifications); // Whitelist changed
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Pop().GetInteger());
            Assert.AreEqual(1045260, engine.FeeConsumed);

            // Clean white list

            engine.SnapshotCache.Commit();
            engine = CreateEngineWithCommitteeSigner(snapshotCache, script);

            Assert.AreEqual(1, NativeContract.Policy.CleanWhitelist(engine, NativeContract.NEO.GetContractState(engine.ProtocolSettings, engine.SnapshotCache, 0)));
            Assert.HasCount(1, engine.Notifications); // Whitelist deleted
        }

        [TestMethod]
        public void TestSetWhiteListFeeContractNegativeFixedFee()
        {
            var snapshotCache = _snapshotCache.CloneCache();
            var engine = CreateEngineWithCommitteeSigner(snapshotCache);

            // Register a dummy contract
            UInt160 contractHash;
            using (var sb = new ScriptBuilder())
            {
                sb.Emit(OpCode.RET);
                var script = sb.ToArray();
                contractHash = script.ToScriptHash();
                snapshotCache.DeleteContract(contractHash);
                var manifest = TestUtils.CreateManifest("dummy", ContractParameterType.Any);
                manifest.Abi.Methods = [
                    new ContractMethodDescriptor
                    {
                        Name = "foo",
                        Parameters = [],
                        ReturnType = ContractParameterType.Any,
                        Offset = 0,
                        Safe = false
                    }
                ];

                var contract = TestUtils.GetContract(script, manifest);
                snapshotCache.AddContract(contractHash, contract);
            }

            // Invoke SetWhiteListFeeContract with fixedFee negative

            Assert.Throws<ArgumentOutOfRangeException>(() => NativeContract.Policy.SetWhitelistFeeContract(engine, contractHash, "foo", 1, -1L));
        }

        [TestMethod]
        public void TestSetWhiteListFeeContractWhenContractNotFound()
        {
            var snapshotCache = _snapshotCache.CloneCache();
            var engine = CreateEngineWithCommitteeSigner(snapshotCache);
            var randomHash = new UInt160(Crypto.Hash160([1, 2, 3]).ToArray());
            Assert.ThrowsExactly<InvalidOperationException>(() => NativeContract.Policy.SetWhitelistFeeContract(engine, randomHash, "transfer", 3, 10));
        }

        [TestMethod]
        public void TestSetWhiteListFeeContractWhenContractNotInAbi()
        {
            var snapshotCache = _snapshotCache.CloneCache();
            var engine = CreateEngineWithCommitteeSigner(snapshotCache);
            Assert.ThrowsExactly<InvalidOperationException>(() => NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, "noexists", 0, 10));
        }

        [TestMethod]
        public void TestSetWhiteListFeeContractWhenArgCountMismatch()
        {
            var snapshotCache = _snapshotCache.CloneCache();
            var engine = CreateEngineWithCommitteeSigner(snapshotCache);
            // transfer exists with 4 args
            Assert.ThrowsExactly<InvalidOperationException>(() => NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, "transfer", 0, 10));
        }

        [TestMethod]
        public void TestSetWhiteListFeeContractWhenNotCommittee()
        {
            var snapshotCache = _snapshotCache.CloneCache();
            var tx = new Transaction
            {
                Version = 0,
                Nonce = 1,
                Signers = [new() { Account = UInt160.Zero, Scopes = WitnessScope.Global }],
                Attributes = [],
                Witnesses = [new Witness { }],
                Script = new byte[1],
                NetworkFee = 0,
                SystemFee = 0,
                ValidUntilBlock = 0
            };

            using var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshotCache, settings: TestProtocolSettings.Default);
            Assert.ThrowsExactly<InvalidOperationException>(() => NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, "transfer", 4, 10));
        }

        [TestMethod]
        public void TestSetWhiteListFeeContractSetContract()
        {
            var snapshotCache = _snapshotCache.CloneCache();
            var engine = CreateEngineWithCommitteeSigner(snapshotCache);
            var method = NativeContract.NEO.GetContractState(engine.ProtocolSettings, engine.SnapshotCache, 0)
                .Manifest.Abi.Methods.Where(u => u.Name == "balanceOf").Single();

            NativeContract.Policy.SetWhitelistFeeContract(engine, NativeContract.NEO.Hash, method.Name, method.Parameters.Length, 123_456);
            Assert.IsTrue(NativeContract.Policy.IsWhitelistFeeContract(engine.SnapshotCache, NativeContract.NEO.Hash, method, out var fixedFee));
            Assert.AreEqual(123_456, fixedFee);
        }

        [TestMethod]
        public void Check_SetTemporaryStorageMaxTTL()
        {
            var snapshot = _snapshotCache.CloneCache();
            Block block = new()
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = null!
                },
                Transactions = []
            };

            // Without committee signature.
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                "setTemporaryStorageMaxTTL", new ContractParameter(ContractParameterType.Integer) { Value = 30_000 });
            });

            var ret = NativeContract.Policy.Call(snapshot, "getTemporaryStorageMaxTTL");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(365 * 24 * 60 * 60 * 1000UL, ret.GetInteger());

            // Too hight value.
            UInt160 committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setTemporaryStorageMaxTTL", new ContractParameter(ContractParameterType.Integer) { Value = PolicyContract.MaxTemporaryStorageMaxTTL + 1 });
            });

            // With signature.
            ret = NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "setTemporaryStorageMaxTTL", new ContractParameter(ContractParameterType.Integer) { Value = 30_000 });
            Assert.IsTrue(ret.IsNull);

            // Check getter.
            ret = NativeContract.Policy.Call(snapshot, "getTemporaryStorageMaxTTL");
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(30_000, ret.GetInteger());
        }

        [TestMethod]
        public void TestWhiteListFee_CallTChargesCaller()
        {
            // CALLT must charge its caller even when the callee has a whitelisted context.
            var snapshotCache = _snapshotCache.CloneCache();

            // Create and whitelist contract A - the callee contract.
            var calleeScript = new byte[] { (byte)OpCode.RET };
            var calleeContract = TestUtils.GetContract(calleeScript, TestUtils.CreateManifest("foo", ContractParameterType.Void));
            snapshotCache.DeleteContract(calleeContract.Hash);
            snapshotCache.AddContract(calleeContract.Hash, calleeContract);

            using (var setupEngine = CreateEngineWithCommitteeSigner(snapshotCache))
            {
                NativeContract.Policy.SetWhitelistFeeContract(setupEngine, calleeContract.Hash, "foo", 0, 0);
                setupEngine.SnapshotCache.Commit();
            }

            // Create contract B - the caller contract that calls whitelisted contract A via CALLT.
            var callerScript = new byte[] { (byte)OpCode.CALLT, 0x00, 0x00 };
            var callerNef = new NefFile
            {
                Compiler = string.Empty,
                Source = string.Empty,
                Tokens =
                [
                    new MethodToken
                    {
                        Hash = calleeContract.Hash,
                        Method = "foo",
                        ParametersCount = 0,
                        HasReturnValue = false,
                        CallFlags = CallFlags.All
                    }
                ],
                Script = callerScript
            };
            callerNef.CheckSum = NefFile.ComputeChecksum(callerNef);

            var callerContract = new ContractState
            {
                Id = 123,
                Hash = callerScript.ToScriptHash(),
                Nef = callerNef,
                Manifest = TestUtils.CreateManifest("main", ContractParameterType.Void)
            };

            // Execute contract B.
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Gorgon, 0).SetItem(Hardfork.HF_Huyao, 0)
            });
            engine.LoadContract(callerContract, callerContract.Manifest.Abi.GetMethod("main", 0)!, CallFlags.All);

            // Check that CALLT price is properly charged.
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual(0, engine.ResultStack.Count);
            // (2205 + 6799.366) * 30 = 270130.98 datoshi, rounded up.
            Assert.AreEqual(270131L, engine.FeeConsumed);
        }

        [TestMethod]
        [DataRow(false, false, 1)]
        [DataRow(false, false, 2)]
        [DataRow(false, true, 1)]
        [DataRow(false, true, 2)]
        [DataRow(true, false, 1)]
        [DataRow(true, false, 2)]
        [DataRow(true, true, 1)]
        [DataRow(true, true, 2)]
        public void TestWhitelistFee_NestedCalls(bool huyao, bool callToken, int calls)
        {
            var snapshot = _snapshotCache.CloneCache();
            var callee = TestUtils.GetContract(new byte[] { (byte)OpCode.RET }, TestUtils.CreateManifest("foo", ContractParameterType.Void));
            snapshot.DeleteContract(callee.Hash);
            snapshot.AddContract(callee.Hash, callee);

            using var script = new ScriptBuilder();
            for (var i = 0; i < calls; i++)
            {
                if (callToken)
                    script.Emit(OpCode.CALLT, new byte[] { 0, 0 });
                else
                {
                    script.EmitDynamicCall(callee.Hash, "foo");
                    script.Emit(OpCode.DROP); // System.Contract.Call returns null for void methods.
                }
            }
            script.Emit(OpCode.RET);
            var caller = TestUtils.GetContract(script.ToArray(), TestUtils.CreateManifest("main", ContractParameterType.Void));
            caller.Nef.Tokens = callToken ? [new MethodToken
            {
                Hash = callee.Hash, Method = "foo", ParametersCount = 0,
                HasReturnValue = false, CallFlags = CallFlags.All
            }] : [];
            caller.Nef.CheckSum = NefFile.ComputeChecksum(caller.Nef);
            snapshot.DeleteContract(caller.Hash);
            snapshot.AddContract(caller.Hash, caller);

            void SetFees(long calleeFee)
            {
                using var setup = CreateEngineWithCommitteeSigner(snapshot);
                NativeContract.Policy.SetWhitelistFeeContract(setup, caller.Hash, "main", 0, 100_000_000);
                NativeContract.Policy.SetWhitelistFeeContract(setup, callee.Hash, "foo", 0, calleeFee);
                setup.SnapshotCache.Commit();
            }

            using var entry = new ScriptBuilder();
            entry.EmitDynamicCall(caller.Hash, "main");
            ApplicationEngine Run(long budget)
            {
                var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot.CloneCache(), gas: budget,
                    settings: TestProtocolSettings.Default with
                    {
                        Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, huyao ? 0u : uint.MaxValue)
                    });
                engine.LoadScript(entry.ToArray());
                engine.Execute();
                return engine;
            }

            // Only the fixed fee changes: opcode exemptions must remain identical.
            SetFees(0);
            using var freeCallee = Run(1_000_000_000);
            Assert.AreEqual(VMState.HALT, freeCallee.State);
            SetFees(200_000_000);
            using var paidCallee = Run(1_000_000_000);
            Assert.AreEqual(VMState.HALT, paidCallee.State);
            Assert.AreEqual(calls * 200_000_000L, paidCallee.FeeConsumed - freeCallee.FeeConsumed);

            using var insufficient = Run(200_000_000);
            Assert.AreEqual(VMState.FAULT, insufficient.State);
            Assert.IsInstanceOfType<InvalidOperationException>(insufficient.FaultException.GetBaseException());
            Assert.AreEqual("Insufficient GAS.", insufficient.FaultException.GetBaseException().Message);

            // Conversion to femtoGAS must not overflow a long before charging the fee.
            SetFees(long.MaxValue);
            using var excessiveFee = Run(1_000_000_000);
            Assert.AreEqual(VMState.FAULT, excessiveFee.State);
            Assert.AreEqual("Insufficient GAS.", excessiveFee.FaultException.GetBaseException().Message);
        }

        private static ApplicationEngine CreateEngineWithCommitteeSigner(DataCache snapshotCache, byte[] script = null)
        {
            // Get committe public keys and calculate m
            var committee = NativeContract.NEO.GetCommittee(snapshotCache);
            var m = (committee.Length / 2) + 1;
            var committeeContract = Contract.CreateMultiSigContract(m, committee);

            // Create Tx needed for CheckWitness / CheckCommittee
            var tx = new Transaction
            {
                Version = 0,
                Nonce = 1,
                Signers = [new() { Account = committeeContract.ScriptHash, Scopes = WitnessScope.Global }],
                Attributes = [],
                Witnesses = [new Witness { InvocationScript = new byte[1], VerificationScript = committeeContract.Script }],
                Script = script ?? [(byte)OpCode.NOP],
                NetworkFee = 0,
                SystemFee = 0,
                ValidUntilBlock = 0
            };

            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Gorgon, 1).SetItem(Hardfork.HF_Huyao, uint.MaxValue)
            };
            var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshotCache, settings: settings);
            engine.LoadScript(tx.Script);

            return engine;
        }

        private static ImmutableDictionary<Hardfork, uint> ConfigThroughHuyao(uint height = 0) =>
            new Dictionary<Hardfork, uint>
            {
                { Hardfork.HF_Aspidochelone, height },
                { Hardfork.HF_Basilisk, height },
                { Hardfork.HF_Cockatrice, height },
                { Hardfork.HF_Domovoi, height },
                { Hardfork.HF_Echidna, height },
                { Hardfork.HF_Faun, height },
                { Hardfork.HF_Gorgon, height },
                { Hardfork.HF_Huyao, height },
            }.ToImmutableDictionary();

        private static ContractParameter HardforkName(string name) =>
            new(ContractParameterType.String) { Value = name };

        private static readonly ContractParameter DefaultActivationDelay = new(ContractParameterType.Integer) { Value = 1 };

        private static StorageKey IaraStorageKey() =>
            StorageKey.Create(NativeContract.Policy.Id, 25 /* Prefix_Hardfork */, Encoding.UTF8.GetBytes("Iara"));

        [TestMethod]
        public void Check_GetHardforkActivationHeight_DefaultUnset()
        {
            var snapshot = _snapshotCache.CloneCache();

            var ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Iara"));
            Assert.IsTrue(ret.IsNull);

            ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Unknown"));
            Assert.IsTrue(ret.IsNull);
        }

        [TestMethod]
        public void Check_ActivateHardfork_WithoutCommittee_Rejected()
        {
            var snapshot = _snapshotCache.CloneCache();
            var block = CreateBlock(1000);

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                    "activateHardfork", HardforkName("Iara"), DefaultActivationDelay);
            });
            Assert.IsFalse(UnknownHardforkException.IsInstance(ex));
            Assert.Contains("committee", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void Check_ActivateHardfork_UnknownWithoutCommittee_DoesNotHalt()
        {
            var snapshot = _snapshotCache.CloneCache();
            var block = CreateBlock(1000);

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                    "activateHardfork", HardforkName("NotAHardfork"), DefaultActivationDelay);
            });
            Assert.IsFalse(UnknownHardforkException.IsInstance(ex),
                "Anyone must not be able to halt the node with an unknown hardfork name.");
        }

        [TestMethod]
        public void Check_ActivateHardfork_RejectsConfigManagedHardfork()
        {
            var snapshot = _snapshotCache.CloneCache();
            var block = CreateBlock(1000);
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "activateHardfork", HardforkName("Huyao"), DefaultActivationDelay);
            });
            Assert.Contains("ProtocolSettings", ex.Message);
        }

        [TestMethod]
        public void Check_ActivateHardfork_UnknownHardfork_StopsNode()
        {
            var snapshot = _snapshotCache.CloneCache();
            var block = CreateBlock(1000);
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            var ex = Assert.ThrowsExactly<UnknownHardforkException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "activateHardfork", HardforkName("NotAHardfork"), DefaultActivationDelay);
            });
            Assert.AreEqual("NotAHardfork", ex.HardforkName);
            Assert.Contains("Update node software", ex.Message);
        }

        [TestMethod]
        public void Check_ActivateHardfork_Iara_NextBlockActivation()
        {
            var snapshot = _snapshotCache.CloneCache();
            const uint blockIndex = 1000;
            var block = CreateBlock(blockIndex);
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            NotifyEventArgs notification = null;
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "activateHardfork",
                (engine, args) => notification = args,
                HardforkName("Iara"), DefaultActivationDelay);

            var ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Iara"));
            Assert.AreEqual(blockIndex + 1, ret.GetInteger());

            Assert.IsFalse(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Iara, blockIndex));
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Iara, blockIndex + 1));

            Assert.IsNotNull(notification);
            Assert.AreEqual("HardforkActivationScheduled", notification.EventName);
            Assert.AreEqual("Iara", notification.State[0].GetString());
            Assert.AreEqual(blockIndex + 1, notification.State[1].GetInteger());
        }

        [TestMethod]
        public void Check_ActivateHardfork_Continuity()
        {
            var snapshot = _snapshotCache.CloneCache();
            const uint blockIndex = 1000;
            var block = CreateBlock(blockIndex);
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // Fail: try to activate Jackalope before Iara activation.
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "activateHardfork",
                HardforkName("Jackalope"), DefaultActivationDelay));
            Assert.Contains("Hardfork-ancestor is not scheduled yet", ex.Message);

            // Good: schedule Jackalope after Iara.
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "activateHardfork",
                HardforkName("Iara"), new(ContractParameterType.Integer) { Value = 5 });
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "activateHardfork",
                HardforkName("Jackalope"), new(ContractParameterType.Integer) { Value = 7 });
            var ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Iara"));
            Assert.AreEqual(blockIndex + 5, ret.GetInteger());
            ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Jackalope"));
            Assert.AreEqual(blockIndex + 7, ret.GetInteger());
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Iara, blockIndex));
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Jackalope, blockIndex));
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Iara, blockIndex + 5));
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Jackalope, blockIndex + 5));
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Iara, blockIndex + 7));
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Jackalope, blockIndex + 7));

            // Fail: reschedule Jackalope prior to Iara.
            ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "activateHardfork",
                HardforkName("Jackalope"), new(ContractParameterType.Integer) { Value = 4 }));
            Assert.Contains("Hardfork Jackalope scheduled at 1004 conflicts with hardfork Iara scheduled at 1005", ex.Message);
        }

        [TestMethod]
        public void Check_ActivateHardfork_NameIsCaseSensitive()
        {
            var snapshot = _snapshotCache.CloneCache();
            var block = CreateBlock(1000);
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            Assert.ThrowsExactly<UnknownHardforkException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "activateHardfork", HardforkName("iara"), DefaultActivationDelay);
            });
            Assert.ThrowsExactly<UnknownHardforkException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "activateHardfork", HardforkName("HF_Iara"), DefaultActivationDelay);
            });

            var ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("iara"));
            Assert.IsTrue(ret.IsNull);
        }

        [TestMethod]
        public void Check_ApplicationEngine_IsHardforkEnabled_UsesPolicy()
        {
            var snapshot = _snapshotCache.CloneCache();
            const uint activationHeight = 42;
            snapshot.Add(IaraStorageKey(), new StorageItem(activationHeight));

            var settings = TestProtocolSettings.Default with { Hardforks = ConfigThroughHuyao() };

            var blockBefore = CreateBlock(activationHeight - 1);
            using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, blockBefore, settings))
            {
                Assert.IsFalse(engine.IsHardforkEnabled(Hardfork.HF_Iara));
            }

            var blockAt = CreateBlock(activationHeight);
            using (var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, blockAt, settings))
            {
                Assert.IsTrue(engine.IsHardforkEnabled(Hardfork.HF_Iara));
            }
        }

        [TestMethod]
        public void Check_HuyaoInitialize_StoresConfigManagedHardforks()
        {
            var snapshot = _snapshotCache.CloneCache();
            Assert.IsTrue(NativeContract.Policy.TryGetActivationHeightFromStorage(snapshot, Hardfork.HF_Huyao, out var huyaoHeight));
            Assert.IsTrue(NativeContract.Policy.TryGetActivationHeightFromStorage(snapshot, Hardfork.HF_Aspidochelone, out _));
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Huyao, huyaoHeight));

            var ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Aspidochelone"));
            Assert.IsFalse(ret.IsNull);
        }

        [TestMethod]
        public void Check_AfterHuyao_ConfigManagedHeightsComeFromPolicy()
        {
            const int huyaoHeight = 7;
            var snapshot = _snapshotCache.CloneCache();
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = new Dictionary<Hardfork, uint>
                {
                    { Hardfork.HF_Aspidochelone, 0 },
                    { Hardfork.HF_Basilisk, 1 },
                    { Hardfork.HF_Cockatrice, 2 },
                    { Hardfork.HF_Domovoi, 3 },
                    { Hardfork.HF_Echidna, 4 },
                    { Hardfork.HF_Faun, 5 },
                    { Hardfork.HF_Gorgon, 6 },
                    { Hardfork.HF_Huyao, huyaoHeight },
                }.ToImmutableDictionary()
            };

            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var witness = new Nep17NativeContractExtensions.ManualWitness(committee);
            var persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = huyaoHeight,
                    MerkleRoot = UInt256.Zero,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    Witness = Witness.Empty,
                },
                Transactions = []
            };

            // Execute OnPersist firstly to initialize Policy storage at Huyao.
            var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            var engine = ApplicationEngine.Create(TriggerType.OnPersist,
                new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr),
                snapshot, persistingBlock, settings: settings);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            snapshot.Commit();

            // Intentionally change the node's settings to distinguish from Policy's ones.
            var malformedSettings = TestProtocolSettings.Default with
            {
                Hardforks = new Dictionary<Hardfork, uint>
                {
                    { Hardfork.HF_Aspidochelone, 10 },
                    { Hardfork.HF_Basilisk, 11 },
                    { Hardfork.HF_Cockatrice, 12 },
                    { Hardfork.HF_Domovoi, 13 },
                    { Hardfork.HF_Echidna, 14 },
                    { Hardfork.HF_Faun, 15 },
                    { Hardfork.HF_Gorgon, 16 },
                    { Hardfork.HF_Huyao, huyaoHeight }, // huyao height should still be preserved since TryGetActivationHeight uses Settings to define if LastConfigManagedHardfork is enabled.
                }.ToImmutableDictionary()
            };

            // Ensure every height is properly stored in the Policy storage.
            for (int hf = (int)Hardfork.HF_Aspidochelone; hf <= (int)Hardfork.HF_Huyao; hf++)
            {
                Assert.IsTrue(NativeContract.Policy.TryGetActivationHeightFromStorage(snapshot, (Hardfork)hf, out var actualHeight));
                Assert.AreEqual((uint)hf, actualHeight);
            }

            // Pre-Huyao: height should be taken from settings.
            for (int hf = (int)Hardfork.HF_Aspidochelone; hf <= (int)Hardfork.HF_Huyao; hf++)
            {
                Assert.IsTrue(PolicyContract.TryGetActivationHeight(malformedSettings, snapshot, (Hardfork)hf, huyaoHeight - 1, out var actualHeight));
                var expected = (uint)(hf + 10);
                if (hf == (int)Hardfork.HF_Huyao)
                    expected = huyaoHeight;
                Assert.AreEqual(expected, actualHeight);
            }

            // Huyao: height should still be taken from settings.
            for (int hf = (int)Hardfork.HF_Aspidochelone; hf <= (int)Hardfork.HF_Huyao; hf++)
            {
                Assert.IsTrue(PolicyContract.TryGetActivationHeight(malformedSettings, snapshot, (Hardfork)hf, huyaoHeight, out var actualHeight));
                var expected = (uint)(hf + 10);
                if (hf == (int)Hardfork.HF_Huyao)
                    expected = huyaoHeight;
                Assert.AreEqual(expected, actualHeight);
            }

            // Post-Huyao: height should be taken from settings.
            for (int hf = (int)Hardfork.HF_Aspidochelone; hf < (int)Hardfork.HF_Huyao; hf++)
            {
                Assert.IsTrue(PolicyContract.TryGetActivationHeight(malformedSettings, snapshot, (Hardfork)hf, huyaoHeight + 1, out var actualHeight));
                Assert.AreEqual((uint)hf, actualHeight);
            }
        }

        [TestMethod]
        public void Check_ActivateHardfork_AlreadyScheduled_Rejected()
        {
            var snapshot = _snapshotCache.CloneCache();
            const uint blockIndex = 1000;
            var block = CreateBlock(blockIndex);
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var witness = new Nep17NativeContractExtensions.ManualWitness(committee);

            NotifyEventArgs notification = null;
            NativeContract.Policy.Call(snapshot, witness, block,
                "activateHardfork",
                (engine, args) => notification = args,
                HardforkName("Iara"), DefaultActivationDelay);

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
                NativeContract.Policy.Call(snapshot, witness, block, "activateHardfork", HardforkName("Iara"), DefaultActivationDelay));
            Assert.Contains("is already scheduled", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void Check_ActivateHardfork_AlreadyEnabled_Rejected()
        {
            var snapshot = _snapshotCache.CloneCache();
            var block = CreateBlock(1000);
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var witness = new Nep17NativeContractExtensions.ManualWitness(committee);

            NativeContract.Policy.Call(snapshot, witness, block, "activateHardfork", HardforkName("Iara"), DefaultActivationDelay);

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
                NativeContract.Policy.Call(snapshot, witness, CreateBlock(1001), "activateHardfork", HardforkName("Iara"), DefaultActivationDelay));
            Assert.Contains("is already scheduled", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void Check_ActivateHardfork_RemoteActivation()
        {
            var snapshot = _snapshotCache.CloneCache();
            uint blockIndex = 10;
            var block = CreateBlock(blockIndex);
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var witness = new Nep17NativeContractExtensions.ManualWitness(committee);
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = ConfigThroughHuyao()
            };

            NativeContract.Policy.Call(snapshot, witness, block, "activateHardfork", HardforkName("Iara"), new(ContractParameterType.Integer) { Value = 3 });

            var ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Iara"));
            Assert.AreEqual(blockIndex + 3, ret.GetInteger());

            Assert.IsFalse(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, blockIndex));
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, blockIndex + 1));
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, blockIndex + 2));
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, blockIndex + 3));
        }

        [TestMethod]
        public void Check_ActivateHardfork_Rescheduling()
        {
            var snapshot = _snapshotCache.CloneCache();
            uint blockIndex = 10;
            var block = CreateBlock(blockIndex);
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var witness = new Nep17NativeContractExtensions.ManualWitness(committee);
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = ConfigThroughHuyao()
            };

            // Schedule at block 13.
            NativeContract.Policy.Call(snapshot, witness, block, "activateHardfork", HardforkName("Iara"), new(ContractParameterType.Integer) { Value = 3 });
            var ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Iara"));
            Assert.AreEqual(blockIndex + 3, ret.GetInteger());

            // Reschedule to block 13 should fail since it's the same block.
            var ex = Assert.Throws<InvalidOperationException>(() =>
                NativeContract.Policy.Call(snapshot, witness, block, "activateHardfork", HardforkName("Iara"), new(ContractParameterType.Integer) { Value = 3 })
            );
            Assert.Contains("is already scheduled", ex.Message, StringComparison.OrdinalIgnoreCase);

            // Reschedule to block 10 should fail since it's the current block (already past).
            var ex1 = Assert.Throws<ArgumentOutOfRangeException>(() =>
                NativeContract.Policy.Call(snapshot, witness, block, "activateHardfork", HardforkName("Iara"), new(ContractParameterType.Integer) { Value = 0 })
            );
            Assert.Contains("Hardfork activation delay should be positive.", ex1.Message, StringComparison.OrdinalIgnoreCase);

            // Reschedule to the earlier block 11 should be OK.
            NativeContract.Policy.Call(snapshot, witness, block, "activateHardfork", HardforkName("Iara"), new(ContractParameterType.Integer) { Value = 1 });
            ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Iara"));
            Assert.AreEqual(blockIndex + 1, ret.GetInteger());

            // Reschedule to the later block 15 should be OK.
            NativeContract.Policy.Call(snapshot, witness, block, "activateHardfork", HardforkName("Iara"), new(ContractParameterType.Integer) { Value = 5 });
            ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Iara"));
            Assert.AreEqual(blockIndex + 5, ret.GetInteger());
        }

        [TestMethod]
        public void Check_ActivateHardfork_SucceedsWithoutLocalSchedule()
        {
            var snapshot = _snapshotCache.CloneCache();
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = ConfigThroughHuyao()
            };
            const uint blockIndex = 200;
            var block = CreateBlock(blockIndex);
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);

            using var engine = ApplicationEngine.Create(
                TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(committee),
                snapshot,
                block,
                settings);
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.Policy.Hash, "activateHardfork", "Iara", 1);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());

            Assert.IsTrue(NativeContract.Policy.TryGetActivationHeightFromStorage(snapshot, Hardfork.HF_Iara, out var height));
            Assert.AreEqual(blockIndex + 1, height);
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, blockIndex + 1));
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, blockIndex));
        }

        [TestMethod]
        public void Check_ApplicationEngine_IsHardforkEnabled_WithoutPersistingBlock()
        {
            var snapshot = _snapshotCache.CloneCache();
            var iaraActivation = snapshot.GetAndChange(IaraStorageKey(), () => new StorageItem(42u));

            var settings = TestProtocolSettings.Default with { Hardforks = ConfigThroughHuyao() };
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, null, settings);
            Assert.IsFalse(engine.IsHardforkEnabled(Hardfork.HF_Iara));
            Assert.IsTrue(engine.IsHardforkEnabled(Hardfork.HF_Huyao));

            iaraActivation.Set(1);
            Assert.IsTrue(engine.IsHardforkEnabled(Hardfork.HF_Iara));
            Assert.IsTrue(engine.IsHardforkEnabled(Hardfork.HF_Huyao));
        }

        [TestMethod]
        public void Check_TryGetActivationHeight_ConfigManagedMissing()
        {
            var snapshot = _snapshotCache.CloneCache();
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = new Dictionary<Hardfork, uint>
                {
                    { Hardfork.HF_Aspidochelone, 0 },
                    { Hardfork.HF_Basilisk, 0 },
                    { Hardfork.HF_Cockatrice, 0 },
                    { Hardfork.HF_Domovoi, 0 },
                    { Hardfork.HF_Echidna, 0 },
                    { Hardfork.HF_Faun, 0 },
                    { Hardfork.HF_Gorgon, 0 },
                }.ToImmutableDictionary()
            };

            Assert.IsFalse(PolicyContract.TryGetActivationHeight(settings, snapshot, Hardfork.HF_Huyao, 0, out _));
            Assert.IsTrue(PolicyContract.TryGetActivationHeight(settings, snapshot, Hardfork.HF_Faun, 0, out var height));
            Assert.AreEqual(0u, height);
            Assert.IsFalse(PolicyContract.TryGetActivationHeight(settings, snapshot, Hardfork.HF_Iara, 0, out _));
        }

        [TestMethod]
        public void Check_GetContractState_WithSnapshot_UsesPolicyHeights()
        {
            var snapshot = _snapshotCache.CloneCache();
            var settings = TestProtocolSettings.Default with { Hardforks = ConfigThroughHuyao() };

            var stateBefore = NativeContract.Policy.GetContractState(settings, snapshot, 0);
            Assert.IsTrue(stateBefore.Manifest.Abi.Methods.Any(m => m.Name == "activateHardfork"));

            var state = NativeContract.Policy.GetContractState(settings, snapshot, 100);
            Assert.IsNotNull(state);
            Assert.AreEqual(NativeContract.Policy.Hash, state.Hash);
        }

        [TestMethod]
        public void Check_GetHardforkActivationHeight_AfterActivate()
        {
            var snapshot = _snapshotCache.CloneCache();
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committee),
                CreateBlock(1000), "activateHardfork", HardforkName("Iara"), DefaultActivationDelay);

            var ret = NativeContract.Policy.Call(snapshot, "getHardforkActivationHeight", HardforkName("Iara"));
            Assert.AreEqual(1001, ret.GetInteger());
        }

        [TestMethod]
        public void Check_TryGetHardforkHeight_Missing()
        {
            var snapshot = _snapshotCache.CloneCache();
            Assert.IsFalse(NativeContract.Policy.TryGetActivationHeightFromStorage(snapshot, Hardfork.HF_Iara, out var height));
            Assert.AreEqual(0u, height);
        }

        [TestMethod]
        public void Check_ActivateHardfork_WithoutPersistingBlock()
        {
            var snapshot = _snapshotCache.CloneCache();
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(committee), snapshot, null, TestProtocolSettings.Default);
            engine.LoadScript(new byte[] { (byte)OpCode.NOP });
            var method = typeof(PolicyContract).GetMethod("ActivateHardfork",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(NativeContract.Policy, [engine, "Iara", (uint)1]));
            Assert.Contains("persisting block", ex.InnerException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void Check_UnknownHardforkException_IsInstance()
        {
            Assert.IsFalse(UnknownHardforkException.IsInstance(null));
            Assert.IsFalse(UnknownHardforkException.IsInstance(new InvalidOperationException("no")));
            var inner = new UnknownHardforkException("Nope");
            Assert.AreEqual("Nope", inner.HardforkName);
            Assert.IsTrue(UnknownHardforkException.IsInstance(new InvalidOperationException("wrap", inner)));
        }

        [TestMethod]
        public void Check_Hardforks_TryParseExact_AndGetName()
        {
            Assert.AreEqual("Iara", Hardforks.GetName(Hardfork.HF_Iara));
            Assert.AreEqual("Huyao", Hardforks.GetName(Hardfork.HF_Huyao));
            Assert.IsFalse(Hardforks.TryParseExact(null, out _));
            Assert.IsFalse(Hardforks.TryParseExact("", out _));
            Assert.IsFalse(Hardforks.TryParseExact("iara", out _));
            Assert.IsFalse(Hardforks.TryParseExact("HF_Iara", out _));
            Assert.IsFalse(Hardforks.TryParseExact("Basilisk, HF_Cockatrice", out _));
            Assert.IsTrue(Hardforks.TryParseExact("Iara", out var hf));
            Assert.AreEqual(Hardfork.HF_Iara, hf);
            Assert.IsTrue(Hardforks.TryParseExact("Basilisk", out var basilisk));
            Assert.AreEqual(Hardfork.HF_Basilisk, basilisk);
        }

        private static Block CreateBlock(uint index) => new()
        {
            Header = new Header
            {
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                Index = index,
                NextConsensus = UInt160.Zero,
                Witness = null!
            },
            Transactions = []
        };
    }
}
