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
using System.Linq;
using System.Numerics;
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
        [DataRow(false)]
        [DataRow(true)]
        public void BlockAccount_GasClaimDependsOnHuyao(bool activated)
        {
            var snapshot = _snapshotCache.CloneCache();
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
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, activated ? 1000u : 1001u)
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
            Assert.AreEqual(activated ? 0u : 1000u, state.BalanceHeight);
        }

        [TestMethod]
        public void BlockAccount_SkipsGasClaimAndPreventsRevoting()
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

            // Blocking must succeed without executing the aborting callback, without claiming GAS.
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
        public void RecoverFund_NeoWithAccruedGasDependsOnHuyao(bool activated)
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

            // Before Huyao, claiming GAS attempts to call the blocked contract and faults.
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
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, activated ? 2000u : 2001u)
            };
            // The stored native ABI must match the simulated hardfork schedule.
            foreach (var native in NativeContract.Contracts)
            {
                snapshot.DeleteContract(native.Hash);
                snapshot.AddContract(native.Hash, native.GetContractState(settings, blockFinish.Index));
            }
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(committeeFullMultiSigAddr), snapshot, blockFinish, settings);
            void Recover() => NativeContract.Policy.Call(engine,
                    "recoverFund",
                    new ContractParameter(ContractParameterType.Hash160) { Value = benignContract.Hash },
                    new ContractParameter(ContractParameterType.Hash160) { Value = NativeContract.NEO.Hash });
            if (!activated)
            {
                var ex = Assert.ThrowsExactly<InvalidOperationException>(Recover);
                Assert.Contains("has been blocked", ex.Message);
                Assert.AreEqual(50, NativeContract.NEO.BalanceOf(snapshot, benignContract.Hash));
            }
            else
            {
                Recover();
                Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.BalanceOf(snapshot, benignContract.Hash));
                Assert.AreEqual(treasuryBefore + 50, NativeContract.NEO.BalanceOf(snapshot, NativeContract.Treasury.Hash));
                Assert.AreEqual(gasBefore, NativeContract.GAS.BalanceOf(snapshot, benignContract.Hash));
            }
        }

        [TestMethod]
        public void BlockedAccount_ReceivingNeoSettlesRewardHeightBeforeUnblock()
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
            var result = NativeContract.NEO.Call(snapshot,
                new Nep17NativeContractExtensions.ManualWitness(sender), block, "transfer",
                new ContractParameter(ContractParameterType.Hash160) { Value = sender },
                new ContractParameter(ContractParameterType.Hash160) { Value = account },
                new ContractParameter(ContractParameterType.Integer) { Value = 50 },
                new ContractParameter(ContractParameterType.Any));
            Assert.IsTrue(result.GetBoolean());
            Assert.AreEqual(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, account));
            Assert.IsGreaterThan(BigInteger.Zero, NativeContract.GAS.BalanceOf(snapshot, sender));
            var state = snapshot.TryGet(accountKey)!.GetInteroperable<NeoToken.NeoAccountState>();
            Assert.AreEqual(new BigInteger(100), state.Balance);
            Assert.AreEqual(1000u, state.BalanceHeight);

            NativeContract.Policy.Call(snapshot, committee, block, "unblockAccount",
                new ContractParameter(ContractParameterType.Hash160) { Value = account });
            block.Header.Index = 1001;
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(account), snapshot, block, TestProtocolSettings.Default);
            var expected = NativeContract.NEO.UnclaimedGas(engine, account, block.Index);
            Assert.IsGreaterThan(BigInteger.Zero, expected);
            result = NativeContract.NEO.Call(engine, "transfer",
                new ContractParameter(ContractParameterType.Hash160) { Value = account },
                new ContractParameter(ContractParameterType.Hash160) { Value = account },
                new ContractParameter(ContractParameterType.Integer) { Value = 0 },
                new ContractParameter(ContractParameterType.Any));
            Assert.IsTrue(result.GetBoolean());
            Assert.AreEqual(expected, NativeContract.GAS.BalanceOf(snapshot, account));
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
            Assert.AreEqual(new UInt160(iter.Value().GetSpan()), UInt160.Zero);
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
            Assert.AreEqual(0, NativeContract.Policy.CleanWhitelist(engine, NativeContract.NEO.GetContractState(ProtocolSettings.Default, 0)));
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

            Assert.AreEqual(1, NativeContract.Policy.CleanWhitelist(engine, NativeContract.NEO.GetContractState(ProtocolSettings.Default, 0)));
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
            var method = NativeContract.NEO.GetContractState(ProtocolSettings.Default, 0)
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
        public void TestWhiteListFee_DynamicOpCodePrice()
        {
            // Ensure that dynamic fee charging mechanism (applied starting from Huyao) properly calculates the price of
            // instruction that loads whitelisted context onto stack.
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
            Assert.AreEqual(engine.OpcodeV1(NativeContract.Policy.GetExecFeeFactor(engine), OpCode.CALLT, new RunStats()), engine.FeeConsumed * ApplicationEngine.OpcodePriceMultiplier); // in the unit of femtoGas.
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
    }
}
