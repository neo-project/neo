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

            var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshotCache, settings: TestProtocolSettings.Default);
            engine.LoadScript(tx.Script);

            return engine;
        }

        #region Hardfork activation (neo#4580)

        /// <summary>
        /// Prefix used by Policy for on-chain hardfork heights (must match PolicyContract).
        /// </summary>
        private const byte Prefix_Hardfork = 24;

        /// <summary>
        /// Prefix used by Policy for the public-network marker (must match PolicyContract).
        /// </summary>
        private const byte Prefix_PublicNetwork = 25;

        /// <summary>Private/dev network magic (not a well-known public network).</summary>
        private const uint PrivateNetworkMagic = 0x4E455654u; // "NETV"

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

        [TestMethod]
        public void Check_GetHardfork_DefaultUnset()
        {
            var snapshot = _snapshotCache.CloneCache();

            var ret = NativeContract.Policy.Call(snapshot, "getHardfork",
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)Hardfork.HF_Iara });
            Assert.IsInstanceOfType(ret, typeof(Integer));
            Assert.AreEqual(-1, ret.GetInteger());
        }

        [TestMethod]
        public void Check_EnableHardfork_WithoutCommittee_Rejected()
        {
            var snapshot = _snapshotCache.CloneCache();
            var block = CreateBlock(1000);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block,
                    "enableHardfork", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)Hardfork.HF_Iara });
            });
        }

        [TestMethod]
        public void Check_EnableHardfork_RejectsConfigManagedHardfork()
        {
            var snapshot = _snapshotCache.CloneCache();
            var block = CreateBlock(1000);
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // Hardforks through Huyao must stay configuration-based.
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "enableHardfork", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)Hardfork.HF_Huyao });
            });
            Assert.Contains("ProtocolSettings", ex.Message);
        }

        [TestMethod]
        public void Check_EnableHardfork_RejectsUnknownHardfork()
        {
            var snapshot = _snapshotCache.CloneCache();
            var block = CreateBlock(1000);
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            // Unknown id: outdated nodes must fail the call (and thus the block) until upgraded.
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                    "enableHardfork", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)byte.MaxValue });
            });
            Assert.Contains("Unknown hardfork", ex.Message);
            Assert.Contains("Update node software", ex.Message);
        }

        [TestMethod]
        public void Check_EnableHardfork_Iara_NextBlockActivation_OnPublicNetwork()
        {
            // TestProtocolSettings.Default uses MainNet magic → public network (Policy-only for Iara).
            var snapshot = _snapshotCache.CloneCache();
            const uint blockIndex = 1000;
            var block = CreateBlock(blockIndex);
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block,
                "enableHardfork", new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)Hardfork.HF_Iara });

            var ret = NativeContract.Policy.Call(snapshot, "getHardfork",
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)Hardfork.HF_Iara });
            Assert.AreEqual(blockIndex + 1, ret.GetInteger());

            Assert.IsFalse(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Iara, blockIndex));
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(TestProtocolSettings.Default, snapshot, Hardfork.HF_Iara, blockIndex + 1));
        }

        [TestMethod]
        public void Check_PublicNetwork_IgnoresLocalPostHuyaoHardforks()
        {
            var snapshot = _snapshotCache.CloneCache();
            // MainNet magic: public. Local Hardforks schedule Iara at 10; Policy has no entry.
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = ConfigThroughHuyao().Add(Hardfork.HF_Iara, 10)
            };

            Assert.IsTrue(settings.IsWellKnownPublicNetwork);
            Assert.IsTrue(PolicyContract.IsPublicNetwork(settings, snapshot));

            // Local config alone would enable at 10, but public rules ignore it.
            Assert.IsTrue(settings.IsHardforkEnabled(Hardfork.HF_Iara, 10));
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, 10));
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, 1_000_000));

            // Policy enables at 50 → only then active.
            snapshot.Add(StorageKey.Create(NativeContract.Policy.Id, Prefix_Hardfork, (byte)Hardfork.HF_Iara), new StorageItem(50u));
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, 49));
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, 50));

            var issues = PolicyContract.GetHardforkConfigurationIssues(settings, snapshot);
            Assert.IsTrue(issues.Any(i => i.Contains("HF_Iara") && i.Contains("!=")));
        }

        [TestMethod]
        public void Check_PrivateNetwork_HardforkDebugOverrides()
        {
            var snapshot = _snapshotCache.CloneCache();
            var settings = TestProtocolSettings.Default with
            {
                Network = PrivateNetworkMagic,
                Hardforks = ConfigThroughHuyao(),
                HardforkDebugOverrides = new Dictionary<Hardfork, uint>
                {
                    { Hardfork.HF_Iara, 25 }
                }.ToImmutableDictionary()
            };

            Assert.IsFalse(settings.IsWellKnownPublicNetwork);
            Assert.IsFalse(PolicyContract.IsPublicNetwork(settings, snapshot));
            ProtocolSettings.ValidateHardforkDebugOverrides(settings); // must not throw

            Assert.IsFalse(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, 24));
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, 25));
        }

        [TestMethod]
        public void Check_HardforkDebugOverrides_ForbiddenOnWellKnownPublicNetwork()
        {
            var settings = TestProtocolSettings.Default with
            {
                HardforkDebugOverrides = new Dictionary<Hardfork, uint>
                {
                    { Hardfork.HF_Iara, 0 }
                }.ToImmutableDictionary()
            };

            var ex = Assert.ThrowsExactly<ArgumentException>(() =>
                ProtocolSettings.ValidateHardforkDebugOverrides(settings));
            Assert.Contains("not allowed on well-known public network", ex.Message);
        }

        [TestMethod]
        public void Check_HardforkDebugOverrides_ForbiddenWhenPublicMarkerSet()
        {
            var snapshot = _snapshotCache.CloneCache();
            snapshot.Add(StorageKey.Create(NativeContract.Policy.Id, Prefix_PublicNetwork), new StorageItem(PrivateNetworkMagic));

            var settings = TestProtocolSettings.Default with
            {
                Network = PrivateNetworkMagic,
                Hardforks = ConfigThroughHuyao(),
                HardforkDebugOverrides = new Dictionary<Hardfork, uint>
                {
                    { Hardfork.HF_Iara, 0 }
                }.ToImmutableDictionary()
            };

            // Shape OK for private magic, but on-chain public marker forbids overrides.
            ProtocolSettings.ValidateHardforkDebugOverrides(settings);

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
                PolicyContract.ValidateHardforkConfiguration(settings, snapshot));
            Assert.Contains("public network", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void Check_SetPublicNetwork_CommitteeOnly_Once()
        {
            var snapshot = _snapshotCache.CloneCache();
            var block = CreateBlock(100);
            var committeeMultiSigAddr = NativeContract.NEO.GetCommitteeAddress(snapshot);

            Assert.AreEqual(-1, NativeContract.Policy.Call(snapshot, "getPublicNetwork").GetInteger());

            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(), block, "setPublicNetwork");
            });

            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block, "setPublicNetwork");
            Assert.AreEqual(TestProtocolSettings.Default.Network, (uint)NativeContract.Policy.Call(snapshot, "getPublicNetwork").GetInteger());

            var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committeeMultiSigAddr), block, "setPublicNetwork");
            });
            Assert.Contains("already set", ex.Message);
        }

        [TestMethod]
        public void Check_OnChainPublicMarker_MakesNetworkPublic()
        {
            var snapshot = _snapshotCache.CloneCache();
            var settings = TestProtocolSettings.Default with
            {
                Network = PrivateNetworkMagic,
                Hardforks = ConfigThroughHuyao().Add(Hardfork.HF_Iara, 0)
            };

            Assert.IsFalse(PolicyContract.IsPublicNetwork(settings, snapshot));
            // Private: Hardforks can schedule Iara.
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, 0));

            snapshot.Add(StorageKey.Create(NativeContract.Policy.Id, Prefix_PublicNetwork), new StorageItem(PrivateNetworkMagic));
            Assert.IsTrue(PolicyContract.IsPublicNetwork(settings, snapshot));
            // After marker: local Hardforks for Iara ignored until Policy enables.
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(settings, snapshot, Hardfork.HF_Iara, 0));
        }

        [TestMethod]
        public void Check_ApplicationEngine_IsHardforkEnabled_UsesPolicy()
        {
            var snapshot = _snapshotCache.CloneCache();
            const uint activationHeight = 42;
            snapshot.Add(StorageKey.Create(NativeContract.Policy.Id, Prefix_Hardfork, (byte)Hardfork.HF_Iara),
                new StorageItem(activationHeight));

            // Public (MainNet magic): Policy only for Iara.
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
        public void Check_DetectHardforkConfigDivergence_HistoricalHeights()
        {
            var local = TestProtocolSettings.Default with
            {
                Network = PrivateNetworkMagic,
                Hardforks = ConfigThroughHuyao(0).SetItem(Hardfork.HF_Basilisk, 100)
            };
            var canonical = TestProtocolSettings.Default with
            {
                Network = PrivateNetworkMagic,
                Hardforks = ConfigThroughHuyao(0).SetItem(Hardfork.HF_Basilisk, 200)
            };

            var issues = ProtocolSettings.DetectHardforkConfigDivergence(local, canonical);
            Assert.IsTrue(issues.Any(i => i.Contains("HF_Basilisk")));
            // Divergent Basilisk heights ⇒ different activation at the same index.
            Assert.IsTrue(PolicyContract.IsHardforkEnabled(local, null, Hardfork.HF_Basilisk, 150));
            Assert.IsFalse(PolicyContract.IsHardforkEnabled(canonical, null, Hardfork.HF_Basilisk, 150));
        }

        [TestMethod]
        public void Check_EnableHardfork_PresentInManifest_AfterHuyao()
        {
            var state = NativeContract.Policy.GetContractState(TestProtocolSettings.Default, 0);
            Assert.IsTrue(state.Manifest.Abi.Methods.Any(m => m.Name == "enableHardfork"));
            Assert.IsTrue(state.Manifest.Abi.Methods.Any(m => m.Name == "getHardfork"));
            Assert.IsTrue(state.Manifest.Abi.Methods.Any(m => m.Name == "getPublicNetwork"));
            Assert.IsTrue(state.Manifest.Abi.Methods.Any(m => m.Name == "setPublicNetwork"));
            Assert.IsTrue(state.Manifest.Abi.Events.Any(e => e.Name == "HardforkEnabled"));
            Assert.IsTrue(state.Manifest.Abi.Events.Any(e => e.Name == "PublicNetworkSet"));
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

        #endregion
    }
}
