// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MemoryPool.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.TestKit.MsTest;
using Moq;
using Neo.Cryptography;
using Neo.Factories;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System.Collections;
using System.Numerics;

namespace Neo.UnitTests.Ledger;

[TestClass]
public class UT_MemoryPool : TestKit
{
    private static NeoSystem _system = null!;

    private const byte Prefix_MaxTransactionsPerBlock = 23;
    private const byte Prefix_FeePerByte = 10;
    private readonly UInt160 senderAccount = UInt160.Zero;
    private MemoryPool _unit = null!;

    [ClassInitialize]
    public static void TestSetup(TestContext ctx)
    {
        _system = TestBlockchain.GetSystem();
    }

    private static DataCache GetSnapshot()
    {
        return _system.StoreView.CloneCache();
    }

    [TestInitialize]
    public void TestSetup()
    {
        // protect against external changes on TimeProvider
        TimeProvider.ResetToDefault();

        // Create a MemoryPool with capacity of 100
        _unit = new MemoryPool(new NeoSystem(TestProtocolSettings.Default with { MemoryPoolMaxTransactions = 100 }, storageProvider: (string?)null));

        // Verify capacity equals the amount specified
        Assert.AreEqual(100, _unit.Capacity);

        Assert.AreEqual(0, _unit.VerifiedCount);
        Assert.AreEqual(0, _unit.UnVerifiedCount);
        Assert.IsEmpty(_unit);
    }

    private Transaction CreateTransactionWithFee(long fee)
    {
        var randomBytes = RandomNumberFactory.NextBytes(16);
        Mock<Transaction> mock = new();
        mock.Setup(p => p.VerifyStateDependent(
                It.IsAny<ProtocolSettings>(),
                It.IsAny<DataCache>(),
                It.IsAny<TransactionVerificationContext>(),
                It.IsAny<IEnumerable<Transaction>>()))
            .Returns(VerifyResult.Succeed);
        mock.Setup(p => p.VerifyStateIndependent(It.IsAny<ProtocolSettings>())).Returns(VerifyResult.Succeed);
        mock.Object.Script = randomBytes;
        mock.Object.NetworkFee = fee;
        mock.Object.Attributes = [];
        mock.Object.Signers = [new() { Account = senderAccount, Scopes = WitnessScope.None }];
        mock.Object.Witnesses = [Witness.Empty];
        return mock.Object;
    }

    private Transaction CreateTransactionWithFeeAndBalanceVerify(long fee)
    {
        var randomBytes = RandomNumberFactory.NextBytes(16);
        Mock<Transaction> mock = new();
        UInt160 sender = senderAccount;
        mock.Setup(p => p.VerifyStateDependent(
                It.IsAny<ProtocolSettings>(),
                It.IsAny<DataCache>(),
                It.IsAny<TransactionVerificationContext>(),
                It.IsAny<IEnumerable<Transaction>>()))
            .Returns((ProtocolSettings settings, DataCache snapshot, TransactionVerificationContext context, IEnumerable<Transaction> conflictsList)
                => context.CheckTransaction(mock.Object, conflictsList, snapshot) ? VerifyResult.Succeed : VerifyResult.InsufficientFunds);

        mock.Setup(p => p.VerifyStateIndependent(It.IsAny<ProtocolSettings>())).Returns(VerifyResult.Succeed);
        mock.Object.Script = randomBytes;
        mock.Object.NetworkFee = fee;
        mock.Object.Attributes = [];
        mock.Object.Signers = [new() { Account = senderAccount, Scopes = WitnessScope.None }];
        mock.Object.Witnesses = [Witness.Empty];
        return mock.Object;
    }

    private Transaction CreateTransaction(long fee = -1)
    {
        if (fee != -1)
            return CreateTransactionWithFee(fee);
        return CreateTransactionWithFee(RandomNumberFactory.NextInt64(100000, 100000000));
    }

    private void AddTransactions(int count)
    {
        var snapshot = GetSnapshot();
        for (int i = 0; i < count; i++)
        {
            var txToAdd = CreateTransaction();
            _unit.TryAdd(txToAdd, snapshot);
        }

        Console.WriteLine($"created {count} tx");
    }

    private void AddTransaction(Transaction txToAdd)
    {
        var snapshot = GetSnapshot();
        _unit.TryAdd(txToAdd, snapshot);
    }

    private void AddTransactionsWithBalanceVerify(int count, long fee, DataCache snapshot)
    {
        for (int i = 0; i < count; i++)
        {
            var txToAdd = CreateTransactionWithFeeAndBalanceVerify(fee);
            _unit.TryAdd(txToAdd, snapshot);
        }

        Console.WriteLine($"created {count} tx");
    }

    [TestMethod]
    public void CapacityTest()
    {
        // Add over the capacity items, verify that the verified count increases each time
        AddTransactions(101);

        Console.WriteLine($"VerifiedCount: {_unit.VerifiedCount} Count {_unit.SortedTxCount}");

        Assert.AreEqual(100, _unit.SortedTxCount);
        Assert.AreEqual(100, _unit.VerifiedCount);
        Assert.AreEqual(0, _unit.UnVerifiedCount);
        Assert.HasCount(100, _unit);
    }

    [TestMethod]
    public void BlockPersistMovesTxToUnverifiedAndReverification()
    {
        AddTransactions(70);

        Assert.AreEqual(70, _unit.SortedTxCount);

        var block = new Block
        {
            Header = new Header
            {
                PrevHash = null!,
                MerkleRoot = null!,
                NextConsensus = null!,
                Witness = null!
            },
            Transactions = _unit.GetSortedVerifiedTransactions(10)
                .Concat(_unit.GetSortedVerifiedTransactions(5)).ToArray()
        };
        _unit.UpdatePoolForBlockPersisted(block, GetSnapshot());
        _unit.InvalidateVerifiedTransactions();
        Assert.AreEqual(0, _unit.SortedTxCount);
        Assert.AreEqual(60, _unit.UnverifiedSortedTxCount);

        _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, GetSnapshot());
        Assert.AreEqual(10, _unit.SortedTxCount);
        Assert.AreEqual(50, _unit.UnverifiedSortedTxCount);

        _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, GetSnapshot());
        Assert.AreEqual(20, _unit.SortedTxCount);
        Assert.AreEqual(40, _unit.UnverifiedSortedTxCount);

        _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, GetSnapshot());
        Assert.AreEqual(30, _unit.SortedTxCount);
        Assert.AreEqual(30, _unit.UnverifiedSortedTxCount);

        _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, GetSnapshot());
        Assert.AreEqual(40, _unit.SortedTxCount);
        Assert.AreEqual(20, _unit.UnverifiedSortedTxCount);

        _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, GetSnapshot());
        Assert.AreEqual(50, _unit.SortedTxCount);
        Assert.AreEqual(10, _unit.UnverifiedSortedTxCount);

        _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(10, GetSnapshot());
        Assert.AreEqual(60, _unit.SortedTxCount);
        Assert.AreEqual(0, _unit.UnverifiedSortedTxCount);
    }

    [TestMethod]
    public async Task BlockPersistAndReverificationWillAbandonTxAsBalanceTransfered()
    {
        var snapshot = GetSnapshot();
        BigInteger balance = NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.GasTokenId, senderAccount);
        ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default, gas: long.MaxValue);
        engine.LoadScript(Array.Empty<byte>());
        await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, balance, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, 70, assertOwner: false, callOnPayment: false);
        long txFee = 1;
        AddTransactionsWithBalanceVerify(70, txFee, engine.SnapshotCache);

        Assert.AreEqual(70, _unit.SortedTxCount);

        var block = new Block
        {
            Header = new Header
            {
                PrevHash = null!,
                MerkleRoot = null!,
                NextConsensus = null!,
                Witness = null!
            },
            Transactions = _unit.GetSortedVerifiedTransactions(10)
        };

        // Simulate the transfer process in tx by burning the balance
        UInt160 sender = block.Transactions[0].Sender;

        ApplicationEngine applicationEngine = ApplicationEngine.Create(TriggerType.All, block, snapshot, block, settings: TestProtocolSettings.Default, gas: (long)balance);
        applicationEngine.LoadScript(Array.Empty<byte>());
        await NativeContract.TokenManagement.BurnInternal(applicationEngine, NativeContract.Governance.GasTokenId, sender, NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.GasTokenId, sender), assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(applicationEngine, NativeContract.Governance.GasTokenId, sender, txFee * 30, assertOwner: false, callOnPayment: false); // Set the balance to meet 30 txs only

        // Persist block and reverify all the txs in mempool, but half of the txs will be discarded
        _unit.UpdatePoolForBlockPersisted(block, applicationEngine.SnapshotCache);
        Assert.AreEqual(30, _unit.SortedTxCount);
        Assert.AreEqual(0, _unit.UnverifiedSortedTxCount);

        // Revert the balance
        await NativeContract.TokenManagement.BurnInternal(applicationEngine, NativeContract.Governance.GasTokenId, sender, txFee * 30, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(applicationEngine, NativeContract.Governance.GasTokenId, sender, balance, assertOwner: false, callOnPayment: false);
    }

    [TestMethod]
    public async Task UpdatePoolForBlockPersisted_RemoveBlockConflicts()
    {
        // Arrange: prepare mempooled and in-bock txs conflicting with each other.
        long txFee = 1;
        var snapshot = GetSnapshot();
        BigInteger balance = NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.GasTokenId, senderAccount);
        ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default, gas: long.MaxValue);
        engine.LoadScript(Array.Empty<byte>());
        await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, balance, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, 7, assertOwner: false, callOnPayment: false); // balance enough for 7 mempooled txs

        var mp1 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // mp1 doesn't conflict with anyone
        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(mp1, engine.SnapshotCache));
        var tx1 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // but in-block tx1 conflicts with mempooled mp1 => mp1 should be removed from pool after persist
        tx1.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp1.Hash } };

        var mp2 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // mp1 and mp2 don't conflict with anyone
        _unit.TryAdd(mp2, engine.SnapshotCache);
        var mp3 = CreateTransactionWithFeeAndBalanceVerify(txFee);
        _unit.TryAdd(mp3, engine.SnapshotCache);
        var tx2 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // in-block tx2 conflicts with mempooled mp2 and mp3 => mp2 and mp3 should be removed from pool after persist
        tx2.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp2.Hash }, new Conflicts() { Hash = mp3.Hash } };

        var tx3 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // in-block tx3 doesn't conflict with anyone
        var mp4 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // mp4 conflicts with in-block tx3 => mp4 should be removed from pool after persist
        mp4.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = tx3.Hash } };
        _unit.TryAdd(mp4, engine.SnapshotCache);

        var tx4 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // in-block tx4 and tx5 don't conflict with anyone
        var tx5 = CreateTransactionWithFeeAndBalanceVerify(txFee);
        var mp5 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // mp5 conflicts with in-block tx4 and tx5 => mp5 should be removed from pool after persist
        mp5.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = tx4.Hash }, new Conflicts() { Hash = tx5.Hash } };
        _unit.TryAdd(mp5, engine.SnapshotCache);

        var mp6 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // mp6 doesn't conflict with anyone and noone conflicts with mp6 => mp6 should be left in the pool after persist
        _unit.TryAdd(mp6, engine.SnapshotCache);

        Assert.AreEqual(6, _unit.SortedTxCount);
        Assert.AreEqual(0, _unit.UnverifiedSortedTxCount);

        var mp7 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // mp7 doesn't conflict with anyone
        var tx6 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // in-block tx6 conflicts with mp7, but doesn't include sender of mp7 into signers list => even if tx6 is included into block, mp7 shouldn't be removed from the pool
        tx6.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp7.Hash } };
        tx6.Signers = new Signer[] { new() { Account = new UInt160(Crypto.Hash160(new byte[] { 1, 2, 3 })) }, new() { Account = new UInt160(Crypto.Hash160(new byte[] { 4, 5, 6 })) } };
        _unit.TryAdd(mp7, engine.SnapshotCache);

        // Act: persist block and reverify all mempooled txs.
        var block = new Block
        {
            Header = new Header
            {
                PrevHash = null!,
                MerkleRoot = null!,
                NextConsensus = null!,
                Witness = null!
            },
            Transactions = new Transaction[] { tx1, tx2, tx3, tx4, tx5, tx6 },
        };
        _unit.UpdatePoolForBlockPersisted(block, engine.SnapshotCache);

        // Assert: conflicting txs should be removed from the pool; the only mp6 that doesn't conflict with anyone should be left.
        Assert.AreEqual(2, _unit.SortedTxCount);
        Assert.IsTrue(_unit.GetSortedVerifiedTransactions().Select(tx => tx.Hash).Contains(mp6.Hash));
        Assert.IsTrue(_unit.GetSortedVerifiedTransactions().Select(tx => tx.Hash).Contains(mp7.Hash));
        Assert.AreEqual(0, _unit.UnverifiedSortedTxCount);

        // Cleanup: revert the balance.
        await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, txFee * 7, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, balance, assertOwner: false, callOnPayment: false);
    }

    [TestMethod]
    public async Task TryAdd_AddRangeOfConflictingTransactions()
    {
        // Arrange: prepare mempooled txs that have conflicts.
        long txFee = 1;
        var maliciousSender = new UInt160(Crypto.Hash160(new byte[] { 1, 2, 3 }));
        var snapshot = GetSnapshot();
        BigInteger balance = NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.GasTokenId, senderAccount);
        ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default, gas: long.MaxValue);
        engine.LoadScript(Array.Empty<byte>());
        await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, balance, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, 100, assertOwner: false, callOnPayment: false); // balance enough for all mempooled txs
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, maliciousSender, 100, assertOwner: false, callOnPayment: false); // balance enough for all mempooled txs

        var mp1 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // mp1 doesn't conflict with anyone and not in the pool yet

        var mp2_1 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // mp2_1 conflicts with mp1 and has the same network fee
        mp2_1.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp1.Hash } };
        _unit.TryAdd(mp2_1, engine.SnapshotCache);
        var mp2_2 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // mp2_2 also conflicts with mp1 and has the same network fee as mp1 and mp2_1
        mp2_2.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp1.Hash } };
        _unit.TryAdd(mp2_2, engine.SnapshotCache);

        var mp3 = CreateTransactionWithFeeAndBalanceVerify(2 * txFee);  // mp3 conflicts with mp1 and has larger network fee
        mp3.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp1.Hash } };
        _unit.TryAdd(mp3, engine.SnapshotCache);

        var mp4 = CreateTransactionWithFeeAndBalanceVerify(3 * txFee);  // mp4 conflicts with mp3 and has larger network fee
        mp4.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp3.Hash } };

        var malicious = CreateTransactionWithFeeAndBalanceVerify(3 * txFee);  // malicious conflicts with mp3 and has larger network fee, but different sender
        malicious.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp3.Hash } };
        malicious.Signers = new Signer[] { new() { Account = new UInt160(Crypto.Hash160(new byte[] { 1, 2, 3 })), Scopes = WitnessScope.None } };

        var mp5 = CreateTransactionWithFeeAndBalanceVerify(2 * txFee);  // mp5 conflicts with mp4 and has smaller network fee
        mp5.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp4.Hash } };

        var mp6 = CreateTransactionWithFeeAndBalanceVerify(mp2_1.NetworkFee + mp2_2.NetworkFee + 1); // mp6 conflicts with mp2_1 and mp2_2 and has larger network fee.
        mp6.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp2_1.Hash }, new Conflicts() { Hash = mp2_2.Hash } };

        var mp7 = CreateTransactionWithFeeAndBalanceVerify(txFee * 2 + 1); // mp7 doesn't conflicts with anyone, but mp8, mp9 and mp10malicious has smaller sum network fee and conflict with mp7.
        var mp8 = CreateTransactionWithFeeAndBalanceVerify(txFee);
        mp8.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp7.Hash } };
        var mp9 = CreateTransactionWithFeeAndBalanceVerify(txFee);
        mp9.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp7.Hash } };
        var mp10malicious = CreateTransactionWithFeeAndBalanceVerify(txFee);
        mp10malicious.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp7.Hash } };
        mp10malicious.Signers = new Signer[] { new() { Account = maliciousSender, Scopes = WitnessScope.None } };

        Assert.AreEqual(3, _unit.SortedTxCount);
        Assert.AreEqual(0, _unit.UnverifiedSortedTxCount);

        // Act & Assert: try to add conlflicting transactions to the pool.
        Assert.AreEqual(VerifyResult.HasConflicts, _unit.TryAdd(mp1, engine.SnapshotCache)); // mp1 conflicts with mp2_1, mp2_2 and mp3 but has lower network fee than mp3 => mp1 fails to be added
        Assert.AreEqual(3, _unit.SortedTxCount);
        CollectionAssert.IsSubsetOf(new List<Transaction>() { mp2_1, mp2_2, mp3 }, _unit.GetVerifiedTransactions().ToList());

        Assert.AreEqual(VerifyResult.HasConflicts, _unit.TryAdd(malicious, engine.SnapshotCache)); // malicious conflicts with mp3, has larger network fee but malicious (different) sender => mp3 shoould be left in pool
        Assert.AreEqual(3, _unit.SortedTxCount);
        CollectionAssert.IsSubsetOf(new List<Transaction>() { mp2_1, mp2_2, mp3 }, _unit.GetVerifiedTransactions().ToList());

        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(mp4, engine.SnapshotCache)); // mp4 conflicts with mp3 and has larger network fee => mp3 shoould be removed from pool
        Assert.AreEqual(3, _unit.SortedTxCount);
        CollectionAssert.IsSubsetOf(new List<Transaction>() { mp2_1, mp2_2, mp4 }, _unit.GetVerifiedTransactions().ToList());

        Assert.AreEqual(VerifyResult.HasConflicts, _unit.TryAdd(mp1, engine.SnapshotCache)); // mp1 conflicts with mp2_1 and mp2_2 and has same network fee => mp2_1 and mp2_2 should be left in pool.
        Assert.AreEqual(3, _unit.SortedTxCount);
        CollectionAssert.IsSubsetOf(new List<Transaction>() { mp2_1, mp2_2, mp4 }, _unit.GetVerifiedTransactions().ToList());

        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(mp6, engine.SnapshotCache)); // mp6 conflicts with mp2_1 and mp2_2 and has larger network fee than the sum of mp2_1 and mp2_2 fees => mp6 should be added.
        Assert.AreEqual(2, _unit.SortedTxCount);
        CollectionAssert.IsSubsetOf(new List<Transaction>() { mp6, mp4 }, _unit.GetVerifiedTransactions().ToList());

        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(mp1, engine.SnapshotCache)); // mp1 conflicts with mp2_1 and mp2_2, but they are not in the pool now => mp1 should be added.
        Assert.AreEqual(3, _unit.SortedTxCount);
        CollectionAssert.IsSubsetOf(new List<Transaction>() { mp1, mp6, mp4 }, _unit.GetVerifiedTransactions().ToList());

        Assert.AreEqual(VerifyResult.HasConflicts, _unit.TryAdd(mp2_1, engine.SnapshotCache)); // mp2_1 conflicts with mp1 and has same network fee => mp2_1 shouldn't be added to the pool.
        Assert.AreEqual(3, _unit.SortedTxCount);
        CollectionAssert.IsSubsetOf(new List<Transaction>() { mp1, mp6, mp4 }, _unit.GetVerifiedTransactions().ToList());

        Assert.AreEqual(VerifyResult.HasConflicts, _unit.TryAdd(mp5, engine.SnapshotCache)); // mp5 conflicts with mp4 and has smaller network fee => mp5 fails to be added.
        Assert.AreEqual(3, _unit.SortedTxCount);
        CollectionAssert.IsSubsetOf(new List<Transaction>() { mp1, mp6, mp4 }, _unit.GetVerifiedTransactions().ToList());

        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(mp8, engine.SnapshotCache)); // mp8, mp9 and mp10malicious conflict with mp7, but mo7 is not in the pool yet.
        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(mp9, engine.SnapshotCache));
        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(mp10malicious, engine.SnapshotCache));
        Assert.AreEqual(6, _unit.SortedTxCount);
        CollectionAssert.IsSubsetOf(new List<Transaction>() { mp1, mp6, mp4, mp8, mp9, mp10malicious }, _unit.GetVerifiedTransactions().ToList());
        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(mp7, engine.SnapshotCache)); // mp7 has larger network fee than the sum of mp8 and mp9 fees => should be added to the pool.
        Assert.AreEqual(4, _unit.SortedTxCount);
        CollectionAssert.IsSubsetOf(new List<Transaction>() { mp1, mp6, mp4, mp7 }, _unit.GetVerifiedTransactions().ToList());

        // Cleanup: revert the balance.
        await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, 100, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, balance, assertOwner: false, callOnPayment: false);
        await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, maliciousSender, 100, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, maliciousSender, balance, assertOwner: false, callOnPayment: false);
    }

    [TestMethod]
    public async Task TryRemoveVerified_RemoveVerifiedTxWithConflicts()
    {
        // Arrange: prepare mempooled txs that have conflicts.
        long txFee = 1;
        var snapshot = GetSnapshot();
        BigInteger balance = NativeContract.TokenManagement.BalanceOf(snapshot, NativeContract.Governance.GasTokenId, senderAccount);
        ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: TestProtocolSettings.Default, gas: long.MaxValue);
        engine.LoadScript(Array.Empty<byte>());
        await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, balance, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, 100, assertOwner: false, callOnPayment: false); // balance enough for all mempooled txs

        var mp1 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // mp1 doesn't conflict with anyone and not in the pool yet

        var mp2 = CreateTransactionWithFeeAndBalanceVerify(2 * txFee);  // mp2 conflicts with mp1 and has larger same network fee
        mp2.Attributes = new TransactionAttribute[] { new Conflicts() { Hash = mp1.Hash } };
        _unit.TryAdd(mp2, engine.SnapshotCache);

        Assert.AreEqual(1, _unit.SortedTxCount);
        Assert.AreEqual(0, _unit.UnverifiedSortedTxCount);

        Assert.AreEqual(VerifyResult.HasConflicts, _unit.TryAdd(mp1, engine.SnapshotCache)); // mp1 conflicts with mp2 but has lower network fee
        Assert.AreEqual(1, _unit.SortedTxCount);
        CollectionAssert.Contains(_unit.GetVerifiedTransactions().ToList(), mp2);

        // Act & Assert: try to invalidate verified transactions and push conflicting one.
        _unit.InvalidateVerifiedTransactions();
        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(mp1, engine.SnapshotCache)); // mp1 conflicts with mp2 but mp2 is not verified anymore
        Assert.AreEqual(1, _unit.SortedTxCount);
        CollectionAssert.Contains(_unit.GetVerifiedTransactions().ToList(), mp1);

        var tx1 = CreateTransactionWithFeeAndBalanceVerify(txFee);  // in-block tx1 doesn't conflict with anyone and is aimed to trigger reverification
        var block = new Block
        {
            Header = new Header
            {
                PrevHash = null!,
                MerkleRoot = null!,
                NextConsensus = null!,
                Witness = null!
            },
            Transactions = new Transaction[] { tx1 },
        };
        _unit.UpdatePoolForBlockPersisted(block, engine.SnapshotCache);
        Assert.AreEqual(1, _unit.SortedTxCount);
        CollectionAssert.Contains(_unit.GetVerifiedTransactions().ToList(), mp2); // after reverificaion mp2 should be back at verified list; mp1 should be completely kicked off
    }

    private static void VerifyTransactionsSortedDescending(IEnumerable<Transaction> transactions)
    {
        Transaction? lastTransaction = null;
        foreach (var tx in transactions)
        {
            if (lastTransaction != null)
            {
                if (lastTransaction.FeePerByte == tx.FeePerByte)
                {
                    if (lastTransaction.NetworkFee == tx.NetworkFee)
                        Assert.IsTrue(lastTransaction.Hash < tx.Hash);
                    else
                        Assert.IsGreaterThan(tx.NetworkFee, lastTransaction.NetworkFee);
                }
                else
                {
                    Assert.IsGreaterThan(tx.FeePerByte, lastTransaction.FeePerByte);
                }
            }
            lastTransaction = tx;
        }
    }

    [TestMethod]
    public void VerifySortOrderAndThatHighetFeeTransactionsAreReverifiedFirst()
    {
        AddTransactions(100);

        var sortedVerifiedTxs = _unit.GetSortedVerifiedTransactions();
        // verify all 100 transactions are returned in sorted order
        Assert.HasCount(100, sortedVerifiedTxs);
        VerifyTransactionsSortedDescending(sortedVerifiedTxs);

        // move all to unverified
        var block = new Block
        {
            Header = new Header
            {
                PrevHash = null!,
                MerkleRoot = null!,
                NextConsensus = null!,
                Witness = null!
            },
            Transactions = Array.Empty<Transaction>()
        };
        _unit.UpdatePoolForBlockPersisted(block, GetSnapshot());
        _unit.InvalidateVerifiedTransactions();
        Assert.AreEqual(0, _unit.SortedTxCount);
        Assert.AreEqual(100, _unit.UnverifiedSortedTxCount);

        // We can verify the order they are re-verified by reverifying 2 at a time
        while (_unit.UnVerifiedCount > 0)
        {
            _unit.GetVerifiedAndUnverifiedTransactions(out var sortedVerifiedTransactions, out var sortedUnverifiedTransactions);
            Assert.AreEqual(0, sortedVerifiedTransactions.Count());
            var sortedUnverifiedArray = sortedUnverifiedTransactions.ToArray();
            VerifyTransactionsSortedDescending(sortedUnverifiedArray);
            var maxTransaction = sortedUnverifiedArray.First();
            var minTransaction = sortedUnverifiedArray.Last();

            // reverify 1 high priority and 1 low priority transaction
            _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(1, GetSnapshot());
            var verifiedTxs = _unit.GetSortedVerifiedTransactions();
            Assert.HasCount(1, verifiedTxs);
            Assert.AreEqual(maxTransaction, verifiedTxs[0]);
            var blockWith2Tx = new Block
            {
                Header = new Header
                {
                    PrevHash = null!,
                    MerkleRoot = null!,
                    NextConsensus = null!,
                    Witness = null!
                },
                Transactions = new[] { maxTransaction, minTransaction }
            };
            // verify and remove the 2 transactions from the verified pool
            _unit.UpdatePoolForBlockPersisted(blockWith2Tx, GetSnapshot());
            _unit.InvalidateVerifiedTransactions();
            Assert.AreEqual(0, _unit.SortedTxCount);
        }
        Assert.AreEqual(0, _unit.UnverifiedSortedTxCount);
    }

    void VerifyCapacityThresholdForAttemptingToAddATransaction()
    {
        var sortedVerified = _unit.GetSortedVerifiedTransactions();
        var txBarelyWontFit = CreateTransactionWithFee(sortedVerified.Last().NetworkFee - 1);
        Assert.IsFalse(_unit.CanTransactionFitInPool(txBarelyWontFit));
        var txBarelyFits = CreateTransactionWithFee(sortedVerified.Last().NetworkFee + 1);
        Assert.IsTrue(_unit.CanTransactionFitInPool(txBarelyFits));
    }

    [TestMethod]
    public void VerifyCanTransactionFitInPoolWorksAsIntended()
    {
        AddTransactions(100);
        VerifyCapacityThresholdForAttemptingToAddATransaction();
        AddTransactions(50);
        VerifyCapacityThresholdForAttemptingToAddATransaction();
        AddTransactions(50);
        VerifyCapacityThresholdForAttemptingToAddATransaction();
    }

    [TestMethod]
    public void CapacityTestWithUnverifiedHighProirtyTransactions()
    {
        // Verify that unverified high priority transactions will not be pushed out of the queue by incoming
        // low priority transactions

        // Fill pool with high priority transactions
        AddTransactions(99);

        // move all to unverified
        var block = new Block
        {
            Header = new Header
            {
                PrevHash = null!,
                MerkleRoot = null!,
                NextConsensus = null!,
                Witness = null!
            },
            Transactions = Array.Empty<Transaction>()
        };
        _unit.UpdatePoolForBlockPersisted(block, GetSnapshot());

        Assert.IsTrue(_unit.CanTransactionFitInPool(CreateTransaction()));
        AddTransactions(1);
        Assert.IsFalse(_unit.CanTransactionFitInPool(CreateTransactionWithFee(0)));
    }

    [TestMethod]
    public void TestInvalidateAll()
    {
        AddTransactions(30);

        Assert.AreEqual(0, _unit.UnverifiedSortedTxCount);
        Assert.AreEqual(30, _unit.SortedTxCount);
        _unit.InvalidateAllTransactions();
        Assert.AreEqual(30, _unit.UnverifiedSortedTxCount);
        Assert.AreEqual(0, _unit.SortedTxCount);
    }

    [TestMethod]
    public void TestContainsKey()
    {
        var snapshot = GetSnapshot();
        AddTransactions(10);

        var txToAdd = CreateTransaction();
        _unit.TryAdd(txToAdd, snapshot);
        Assert.IsTrue(_unit.ContainsKey(txToAdd.Hash));
        _unit.InvalidateVerifiedTransactions();
        Assert.IsTrue(_unit.ContainsKey(txToAdd.Hash));
    }

    [TestMethod]
    public void TestGetEnumerator()
    {
        AddTransactions(10);
        _unit.InvalidateVerifiedTransactions();
        IEnumerator<Transaction> enumerator = _unit.GetEnumerator();
        foreach (Transaction tx in _unit)
        {
            enumerator.MoveNext();
            Assert.IsTrue(ReferenceEquals(tx, enumerator.Current));
        }
    }

    [TestMethod]
    public void TestIEnumerableGetEnumerator()
    {
        AddTransactions(10);
        _unit.InvalidateVerifiedTransactions();
        IEnumerable enumerable = _unit;
        var enumerator = enumerable.GetEnumerator();
        foreach (Transaction tx in _unit)
        {
            enumerator.MoveNext();
            Assert.IsTrue(ReferenceEquals(tx, enumerator.Current));
        }
    }

    [TestMethod]
    public void TestGetVerifiedTransactions()
    {
        var snapshot = GetSnapshot();
        var tx1 = CreateTransaction();
        var tx2 = CreateTransaction();
        _unit.TryAdd(tx1, snapshot);
        _unit.InvalidateVerifiedTransactions();
        _unit.TryAdd(tx2, snapshot);
        IEnumerable<Transaction> enumerable = _unit.GetVerifiedTransactions();
        Assert.AreEqual(1, enumerable.Count());
        var enumerator = enumerable.GetEnumerator();
        enumerator.MoveNext();
        Assert.AreEqual(tx2, enumerator.Current);
    }

    [TestMethod]
    public void TestReVerifyTopUnverifiedTransactionsIfNeeded()
    {
        _unit = new MemoryPool(new NeoSystem(TestProtocolSettings.Default with { MemoryPoolMaxTransactions = 600 }, storageProvider: (string?)null));

        AddTransaction(CreateTransaction(100000001));
        AddTransaction(CreateTransaction(100000001));
        AddTransaction(CreateTransaction(100000001));
        AddTransaction(CreateTransaction(1));
        Assert.AreEqual(4, _unit.VerifiedCount);
        Assert.AreEqual(0, _unit.UnVerifiedCount);

        _unit.InvalidateVerifiedTransactions();
        Assert.AreEqual(0, _unit.VerifiedCount);
        Assert.AreEqual(4, _unit.UnVerifiedCount);

        AddTransactions(511); // Max per block currently is 512
        Assert.AreEqual(511, _unit.VerifiedCount);
        Assert.AreEqual(4, _unit.UnVerifiedCount);

        var result = _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(1, GetSnapshot());
        Assert.IsTrue(result);
        Assert.AreEqual(512, _unit.VerifiedCount);
        Assert.AreEqual(3, _unit.UnVerifiedCount);

        result = _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(2, GetSnapshot());
        Assert.IsTrue(result);
        Assert.AreEqual(514, _unit.VerifiedCount);
        Assert.AreEqual(1, _unit.UnVerifiedCount);

        result = _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(3, GetSnapshot());
        Assert.IsFalse(result);
        Assert.AreEqual(515, _unit.VerifiedCount);
        Assert.AreEqual(0, _unit.UnVerifiedCount);
    }

    [TestMethod]
    public void TestTryAdd()
    {
        var snapshot = GetSnapshot();
        var tx1 = CreateTransaction();
        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(tx1, snapshot));
        Assert.AreNotEqual(VerifyResult.Succeed, _unit.TryAdd(tx1, snapshot));
    }

    [TestMethod]
    public void TestTryGetValue()
    {
        var snapshot = GetSnapshot();
        var tx1 = CreateTransaction();
        _unit.TryAdd(tx1, snapshot);
        Assert.IsTrue(_unit.TryGetValue(tx1.Hash, out Transaction? tx));
        Assert.AreEqual(tx1, tx);

        _unit.InvalidateVerifiedTransactions();
        Assert.IsTrue(_unit.TryGetValue(tx1.Hash, out tx));
        Assert.AreEqual(tx1, tx);

        var tx2 = CreateTransaction();
        Assert.IsFalse(_unit.TryGetValue(tx2.Hash, out _));
    }

    [TestMethod]
    public void TestUpdatePoolForBlockPersisted()
    {
        var snapshot = GetSnapshot();
        byte[] transactionsPerBlock = { 0x18, 0x00, 0x00, 0x00 }; // 24
        byte[] feePerByte = { 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00 }; // 1048576
        StorageItem item1 = new()
        {
            Value = transactionsPerBlock
        };
        StorageItem item2 = new()
        {
            Value = feePerByte
        };
        var key1 = CreateStorageKey(NativeContract.Policy.Id, Prefix_MaxTransactionsPerBlock);
        var key2 = CreateStorageKey(NativeContract.Policy.Id, Prefix_FeePerByte);
        snapshot.Add(key1, item1);
        snapshot.Add(key2, item2);

        var tx1 = CreateTransaction();
        var tx2 = CreateTransaction();
        Transaction[] transactions = { tx1, tx2 };
        _unit.TryAdd(tx1, snapshot);

        var block = new Block
        {
            Header = new Header
            {
                PrevHash = null!,
                MerkleRoot = null!,
                NextConsensus = null!,
                Witness = null!
            },
            Transactions = transactions
        };

        Assert.AreEqual(0, _unit.UnVerifiedCount);
        Assert.AreEqual(1, _unit.VerifiedCount);

        _unit.UpdatePoolForBlockPersisted(block, snapshot);

        Assert.AreEqual(0, _unit.UnVerifiedCount);
        Assert.AreEqual(0, _unit.VerifiedCount);
    }

    [TestMethod]
    public void TestTryRemoveUnVerified()
    {
        AddTransactions(32);
        Assert.AreEqual(32, _unit.SortedTxCount);

        var txs = _unit.GetSortedVerifiedTransactions();
        _unit.InvalidateVerifiedTransactions();

        Assert.AreEqual(0, _unit.SortedTxCount);

        foreach (var tx in txs)
        {
            _unit.TryRemoveUnVerified(tx.Hash, out _);
        }

        Assert.AreEqual(0, _unit.UnVerifiedCount);
    }

    public static StorageKey CreateStorageKey(int id, byte prefix, byte[]? key = null)
    {
        byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
        buffer[0] = prefix;
        key?.CopyTo(buffer.AsSpan(1));
        return new()
        {
            Id = id,
            Key = buffer
        };
    }

    [TestMethod]
    public void TestTransactionAddedEvent()
    {
        // Arrange
        bool eventRaised = false;
        Transaction? capturedTx = null;
        _unit.TransactionAdded += (sender, tx) =>
        {
            eventRaised = true;
            capturedTx = tx;
        };

        var tx = CreateTransaction();
        var snapshot = GetSnapshot();

        // Act
        _unit.TryAdd(tx, snapshot);

        // Assert
        Assert.IsTrue(eventRaised, "TransactionAdded event should be raised");
        Assert.AreEqual(tx, capturedTx, "Transaction in event should match the added transaction");
    }

    [TestMethod]
    public void TestTransactionRemovedEvent()
    {
        // Arrange
        bool eventRaised = false;
        TransactionRemovedEventArgs? capturedArgs = null;
        _unit.TransactionRemoved += (sender, args) =>
        {
            eventRaised = true;
            capturedArgs = args;
        };

        // Add transactions to fill the pool to capacity
        AddTransactions(_unit.Capacity);

        // Add one more to trigger capacity exceeded removal
        var txToAdd = CreateTransaction(long.MaxValue); // High fee to ensure it gets added
        var snapshot = GetSnapshot();

        // Act
        _unit.TryAdd(txToAdd, snapshot);

        // Assert
        Assert.IsTrue(eventRaised, "TransactionRemoved event should be raised");
        Assert.IsNotNull(capturedArgs, "TransactionRemovedEventArgs should not be null");
        Assert.IsGreaterThan(0, capturedArgs.Transactions.Count, "Removed transactions should be included");
        Assert.AreEqual(TransactionRemovalReason.CapacityExceeded, capturedArgs?.Reason,
            "Removal reason should be CapacityExceeded");
    }

    [TestMethod]
    public void TestGetSortedVerifiedTransactionsWithCount()
    {
        // Arrange
        AddTransactions(50);

        // Act - Get subset of transactions
        var transactions10 = _unit.GetSortedVerifiedTransactions(10);
        var transactions20 = _unit.GetSortedVerifiedTransactions(20);
        var transactionsAll = _unit.GetSortedVerifiedTransactions();

        // Assert
        Assert.HasCount(10, transactions10, "Should return exactly 10 transactions");
        Assert.HasCount(20, transactions20, "Should return exactly 20 transactions");
        Assert.HasCount(50, transactionsAll, "Should return all transactions");

        // Verify they are in the right order (highest fee first)
        VerifyTransactionsSortedDescending(transactions10);
        VerifyTransactionsSortedDescending(transactions20);
        VerifyTransactionsSortedDescending(transactionsAll);

        // Verify the first 10 transactions in both sets are the same
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(transactions10[i], transactions20[i],
                "The first 10 transactions should be the same in both result sets");
        }
    }

    [TestMethod]
    public void TestComplexConflictScenario()
    {
        // Arrange
        var snapshot = GetSnapshot();

        // Create a chain of conflicting transactions
        var tx1 = CreateTransaction(100000);
        var tx2 = CreateTransaction(200000);
        var tx3 = CreateTransaction(150000);
        var tx4 = CreateTransaction(300000);

        // Set up conflicts: tx2 conflicts with tx1, tx3 conflicts with tx2, tx4 conflicts with tx3
        tx2.Attributes = [new Conflicts() { Hash = tx1.Hash }];
        tx3.Attributes = [new Conflicts() { Hash = tx2.Hash }];
        tx4.Attributes = [new Conflicts() { Hash = tx3.Hash }];

        // Act & Assert - Add transactions in specific order to test conflict resolution

        // Add tx1 first
        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(tx1, snapshot), "tx1 should be added successfully");
        Assert.HasCount(1, _unit, "Pool should contain 1 transaction");
        Assert.IsTrue(_unit.ContainsKey(tx1.Hash), "Pool should contain tx1");

        // Add tx2 which conflicts with tx1 but has higher fee
        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(tx2, snapshot), "tx2 should be added successfully");
        Assert.HasCount(1, _unit, "Pool should still contain 1 transaction (tx2 replaced tx1)");
        Assert.IsTrue(_unit.ContainsKey(tx2.Hash), "Pool should contain tx2");
        Assert.IsFalse(_unit.ContainsKey(tx1.Hash), "Pool should no longer contain tx1");

        // Add tx3 which conflicts with tx2 but has lower fee
        Assert.AreEqual(VerifyResult.HasConflicts, _unit.TryAdd(tx3, snapshot), "tx3 should not be added due to conflicts");
        Assert.HasCount(1, _unit, "Pool should still contain 1 transaction");
        Assert.IsTrue(_unit.ContainsKey(tx2.Hash), "Pool should still contain tx2");
        Assert.IsFalse(_unit.ContainsKey(tx3.Hash), "Pool should not contain tx3");

        // Add tx4 which conflicts with tx3 (which is not in the pool)
        Assert.AreEqual(VerifyResult.Succeed, _unit.TryAdd(tx4, snapshot), "tx4 should be added successfully");
        Assert.HasCount(2, _unit, "Pool should contain 2 transactions");
        Assert.IsTrue(_unit.ContainsKey(tx2.Hash), "Pool should contain tx2");
        Assert.IsTrue(_unit.ContainsKey(tx4.Hash), "Pool should contain tx4");
    }

    [TestMethod]
    public void TestMultipleConflictsManagement()
    {
        // Arrange
        var snapshot = GetSnapshot();

        // Create a transaction with multiple conflicts
        var tx1 = CreateTransaction(100000);
        var tx2 = CreateTransaction(100000);
        var tx3 = CreateTransaction(100000);

        // Create a transaction that conflicts with all three
        var txMultiConflict = CreateTransaction(350000); // Higher fee than all three combined
        txMultiConflict.Attributes =
        [
            new Conflicts() { Hash = tx1.Hash },
            new Conflicts() { Hash = tx2.Hash },
            new Conflicts() { Hash = tx3.Hash }
        ];

        // Act
        _unit.TryAdd(tx1, snapshot);
        _unit.TryAdd(tx2, snapshot);
        _unit.TryAdd(tx3, snapshot);

        Assert.HasCount(3, _unit, "Should have 3 transactions in the pool");

        // Add the transaction with multiple conflicts
        var result = _unit.TryAdd(txMultiConflict, snapshot);

        // Assert
        Assert.AreEqual(VerifyResult.Succeed, result, "Transaction with multiple conflicts should be added");
        Assert.HasCount(1, _unit, "Should have 1 transaction in the pool after conflicts resolved");
        Assert.IsTrue(_unit.ContainsKey(txMultiConflict.Hash), "Pool should contain the transaction with higher fee");
        Assert.IsFalse(_unit.ContainsKey(tx1.Hash), "Pool should not contain tx1");
        Assert.IsFalse(_unit.ContainsKey(tx2.Hash), "Pool should not contain tx2");
        Assert.IsFalse(_unit.ContainsKey(tx3.Hash), "Pool should not contain tx3");
    }

    [TestMethod]
    public void TestReverificationBehavior()
    {
        // Arrange
        _unit = new MemoryPool(new NeoSystem(TestProtocolSettings.Default with { MemoryPoolMaxTransactions = 1000 }, storageProvider: (string?)null));

        // Add transactions to the pool
        AddTransactions(100);

        // Invalidate all verified transactions to move them to unverified
        _unit.InvalidateVerifiedTransactions();
        Assert.AreEqual(0, _unit.VerifiedCount);
        Assert.AreEqual(100, _unit.UnVerifiedCount);

        // Act
        var snapshot = GetSnapshot();

        // First batch - should reverify some transactions
        _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(20, snapshot);
        var verifiedInFirstBatch = _unit.VerifiedCount;

        // Second batch - should reverify more transactions
        _unit.ReVerifyTopUnverifiedTransactionsIfNeeded(30, snapshot);
        var verifiedInSecondBatch = _unit.VerifiedCount - verifiedInFirstBatch;

        // Assert
        Assert.IsGreaterThan(0, verifiedInFirstBatch, "First batch should reverify some transactions");
        Assert.IsGreaterThan(0, verifiedInSecondBatch, "Second batch should reverify additional transactions");
        Assert.IsLessThan(100, _unit.VerifiedCount, "Not all transactions should be reverified in just two batches");

        // Verify that transactions are reverified in fee order (highest fee first)
        var verifiedTxs = _unit.GetSortedVerifiedTransactions();
        VerifyTransactionsSortedDescending(verifiedTxs);
    }
}
