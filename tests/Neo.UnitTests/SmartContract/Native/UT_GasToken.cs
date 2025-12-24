// Copyright (C) 2015-2025 The Neo Project.
//
// UT_GasToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_GasToken
    {
        private DataCache _snapshotCache;
        private Block _persistingBlock;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
            _persistingBlock = (Block)RuntimeHelpers.GetUninitializedObject(typeof(Block));
        }

        [TestMethod]
        public void Check_Name() => Assert.AreEqual(nameof(GasToken), NativeContract.GAS.Name);

        [TestMethod]
        public void Check_Symbol() => Assert.AreEqual("GAS", NativeContract.GAS.Symbol(_snapshotCache));

        [TestMethod]
        public void Check_Decimals() => Assert.AreEqual(8, NativeContract.GAS.Decimals(_snapshotCache));

        [TestMethod]
        public async Task Check_BalanceOfTransferAndBurn()
        {
            var snapshot = _snapshotCache.CloneCache();
            var persistingBlock = new Block
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
            byte[] from = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators).ToArray();
            byte[] to = new byte[20];
            var supply = NativeContract.GAS.TotalSupply(snapshot);
            Assert.AreEqual(5200000050000000, supply); // 3000000000000000 + 50000000 (neo holder reward)

            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            snapshot.Add(storageKey, new StorageItem(new HashIndexState { Hash = UInt256.Zero, Index = persistingBlock.Index - 1 }));
            var keyCount = snapshot.GetChangeSet().Count();
            // Check unclaim

            var unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
            Assert.AreEqual(new BigInteger(0.5 * 1000 * 100000000L), unclaim.Value);
            Assert.IsTrue(unclaim.State);

            // Transfer

            Assert.IsTrue(NativeContract.NEO.Transfer(snapshot, from, to, BigInteger.Zero, true, persistingBlock));
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = NativeContract.NEO.Transfer(snapshot, from, null, BigInteger.Zero, true, persistingBlock));
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = NativeContract.NEO.Transfer(snapshot, null, to, BigInteger.Zero, false, persistingBlock));
            Assert.AreEqual(100000000, NativeContract.NEO.BalanceOf(snapshot, from));
            Assert.AreEqual(0, NativeContract.NEO.BalanceOf(snapshot, to));

            Assert.AreEqual(52000500_00000000, NativeContract.GAS.BalanceOf(snapshot, from));
            Assert.AreEqual(0, NativeContract.GAS.BalanceOf(snapshot, to));

            // Check unclaim

            unclaim = UT_NeoToken.Check_UnclaimedGas(snapshot, from, persistingBlock);
            Assert.AreEqual(new BigInteger(0), unclaim.Value);
            Assert.IsTrue(unclaim.State);

            supply = NativeContract.GAS.TotalSupply(snapshot);
            Assert.AreEqual(5200050050000000, supply);

            Assert.AreEqual(keyCount + 3, snapshot.GetChangeSet().Count()); // Gas

            // Transfer

            keyCount = snapshot.GetChangeSet().Count();

            Assert.IsFalse(NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000000, false, persistingBlock)); // Not signed
            Assert.IsFalse(NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000001, true, persistingBlock)); // More than balance
            Assert.IsTrue(NativeContract.GAS.Transfer(snapshot, from, to, 52000500_00000000, true, persistingBlock)); // All balance

            // Balance of

            Assert.AreEqual(52000500_00000000, NativeContract.GAS.BalanceOf(snapshot, to));
            Assert.AreEqual(0, NativeContract.GAS.BalanceOf(snapshot, from));

            Assert.AreEqual(keyCount + 1, snapshot.GetChangeSet().Count()); // All

            // Burn

            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default, gas: 0);
            engine.LoadScript(Array.Empty<byte>());

            await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
                await NativeContract.GAS.Burn(engine, new UInt160(to), BigInteger.MinusOne));

            // Burn more than expected

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
                await NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(52000500_00000001)));

            // Real burn

            await NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(1));

            Assert.AreEqual(5200049999999999, NativeContract.GAS.BalanceOf(engine.SnapshotCache, to));

            Assert.AreEqual(2, engine.SnapshotCache.GetChangeSet().Count());

            // Burn all
            await NativeContract.GAS.Burn(engine, new UInt160(to), new BigInteger(5200049999999999));

            Assert.AreEqual(keyCount - 2, engine.SnapshotCache.GetChangeSet().Count());

            // Bad inputs

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = NativeContract.GAS.Transfer(engine.SnapshotCache, from, to, BigInteger.MinusOne, true, persistingBlock));
            Assert.ThrowsExactly<FormatException>(() => _ = NativeContract.GAS.Transfer(engine.SnapshotCache, new byte[19], to, BigInteger.One, false, persistingBlock));
            Assert.ThrowsExactly<FormatException>(() => _ = NativeContract.GAS.Transfer(engine.SnapshotCache, from, new byte[19], BigInteger.One, false, persistingBlock));
        }

        [TestMethod]
        public void Check_OnPersist_AutoClaimUnclaimedGasForFees()
        {
            var snapshot = _snapshotCache.CloneCache();
            var sender = new UInt160(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });
            var persistingBlock = new Block
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };

            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            var currentBlock = snapshot.GetAndChange(storageKey, () => new StorageItem(new HashIndexState()));
            var currentState = currentBlock.GetInteroperable<HashIndexState>();
            currentState.Index = persistingBlock.Index - 1;
            currentState.Hash = UInt256.Zero;

            var neoKey = new KeyBuilder(NativeContract.NEO.Id, 20).Add(sender.ToArray());
            snapshot.Add(neoKey, new StorageItem(new NeoToken.NeoAccountState
            {
                Balance = new BigInteger(1_000_000),
                BalanceHeight = 0,
                VoteTo = null,
                LastGasPerVote = 0
            }));

            var unclaimed = NativeContract.NEO.UnclaimedGas(snapshot, sender, persistingBlock.Index);
            Assert.IsTrue(unclaimed > 0);

            var tx = new Transaction
            {
                Version = 0,
                Nonce = 1,
                SystemFee = 1_00000000,
                NetworkFee = 1_00000000,
                ValidUntilBlock = persistingBlock.Index + 1,
                Signers = [new Signer { Account = sender, Scopes = WitnessScope.None }],
                Attributes = [],
                Script = new byte[] { (byte)OpCode.RET },
                Witnesses = [Witness.Empty]
            };
            persistingBlock.Transactions = [tx];

            var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            using var persistEngine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default);
            persistEngine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, persistEngine.Execute());

            var expected = unclaimed - (tx.SystemFee + tx.NetworkFee);
            Assert.AreEqual(expected, NativeContract.GAS.BalanceOf(snapshot, sender));
        }

        [TestMethod]
        public void Check_OnPersist_AutoClaimSkippedForContractSender()
        {
            var snapshot = _snapshotCache.CloneCache();
            var persistingBlock = new Block
            {
                Header = new Header
                {
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Index = 1000,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };

            var contract = TestUtils.GetContract();
            snapshot.AddContract(contract.Hash, contract);

            var storageKey = new KeyBuilder(NativeContract.Ledger.Id, 12);
            var currentBlock = snapshot.GetAndChange(storageKey, () => new StorageItem(new HashIndexState()));
            var currentState = currentBlock.GetInteroperable<HashIndexState>();
            currentState.Index = persistingBlock.Index - 1;
            currentState.Hash = UInt256.Zero;

            var neoKey = new KeyBuilder(NativeContract.NEO.Id, 20).Add(contract.Hash.ToArray());
            snapshot.Add(neoKey, new StorageItem(new NeoToken.NeoAccountState
            {
                Balance = new BigInteger(1_000_000),
                BalanceHeight = 0,
                VoteTo = null,
                LastGasPerVote = 0
            }));

            var gasKey = new KeyBuilder(NativeContract.GAS.Id, 20).Add(contract.Hash.ToArray());
            snapshot.Add(gasKey, new StorageItem(new AccountState { Balance = BigInteger.Zero }));

            var unclaimed = NativeContract.NEO.UnclaimedGas(snapshot, contract.Hash, persistingBlock.Index);
            Assert.IsTrue(unclaimed > 0);

            var tx = new Transaction
            {
                Version = 0,
                Nonce = 1,
                SystemFee = 1_00000000,
                NetworkFee = 1_00000000,
                ValidUntilBlock = persistingBlock.Index + 1,
                Signers = [new Signer { Account = contract.Hash, Scopes = WitnessScope.None }],
                Attributes = [],
                Script = new byte[] { (byte)OpCode.RET },
                Witnesses = [Witness.Empty]
            };
            persistingBlock.Transactions = [tx];

            var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            using var persistEngine = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, persistingBlock, settings: TestProtocolSettings.Default);
            persistEngine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.FAULT, persistEngine.Execute());
            Assert.IsTrue(persistEngine.FaultException is InvalidOperationException);
            Assert.AreEqual(unclaimed, NativeContract.NEO.UnclaimedGas(snapshot, contract.Hash, persistingBlock.Index));
        }

        internal static StorageKey CreateStorageKey(byte prefix, uint key)
        {
            return CreateStorageKey(prefix, BitConverter.GetBytes(key));
        }

        internal static StorageKey CreateStorageKey(byte prefix, byte[] key = null)
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
            buffer[0] = prefix;
            key?.CopyTo(buffer.AsSpan(1));
            return new()
            {
                Id = NativeContract.GAS.Id,
                Key = buffer
            };
        }
    }
}
