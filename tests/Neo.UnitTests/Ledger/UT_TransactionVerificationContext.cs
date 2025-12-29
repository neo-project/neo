// Copyright (C) 2015-2025 The Neo Project.
//
// UT_TransactionVerificationContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Moq;
using Neo.Factories;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System.Numerics;

namespace Neo.UnitTests.Ledger;

[TestClass]
public class UT_TransactionVerificationContext
{
    private static Transaction CreateTransactionWithFee(long networkFee, long systemFee)
    {
        var randomBytes = RandomNumberFactory.NextBytes(16);
        Mock<Transaction> mock = new();
        mock.Setup(p => p.VerifyStateDependent(It.IsAny<ProtocolSettings>(), It.IsAny<ClonedCache>(), It.IsAny<TransactionVerificationContext>(), It.IsAny<IEnumerable<Transaction>>())).Returns(VerifyResult.Succeed);
        mock.Setup(p => p.VerifyStateIndependent(It.IsAny<ProtocolSettings>())).Returns(VerifyResult.Succeed);
        mock.Object.Script = randomBytes;
        mock.Object.NetworkFee = networkFee;
        mock.Object.SystemFee = systemFee;
        mock.Object.Signers = [new() { Account = UInt160.Zero }];
        mock.Object.Attributes = [];
        mock.Object.Witnesses = [Witness.Empty];
        return mock.Object;
    }

    [TestMethod]
    public async Task TestDuplicateOracle()
    {
        // Fake balance - GasToken uses TokenManagement
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }

        ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestProtocolSettings.Default, gas: long.MaxValue);
        engine.LoadScript(Array.Empty<byte>());
        BigInteger balance = NativeContract.TokenManagement.BalanceOf(snapshotCache, NativeContract.Governance.GasTokenId, UInt160.Zero);
        await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, balance, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, 8, assertOwner: false, callOnPayment: false);

        // Test
        TransactionVerificationContext verificationContext = new();
        var tx = CreateTransactionWithFee(1, 2);
        tx.Attributes = [new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = 1, Result = Array.Empty<byte>() }];
        var conflicts = new List<Transaction>();
        Assert.IsTrue(verificationContext.CheckTransaction(tx, conflicts, engine.SnapshotCache));
        verificationContext.AddTransaction(tx);

        tx = CreateTransactionWithFee(2, 1);
        tx.Attributes = [new OracleResponse() { Code = OracleResponseCode.ConsensusUnreachable, Id = 1, Result = Array.Empty<byte>() }];
        Assert.IsFalse(verificationContext.CheckTransaction(tx, conflicts, snapshotCache));
    }

    [TestMethod]
    public async Task TestTransactionSenderFee()
    {
        // GasToken uses TokenManagement
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }

        ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestProtocolSettings.Default, gas: long.MaxValue);
        engine.LoadScript(Array.Empty<byte>());
        BigInteger balance = NativeContract.TokenManagement.BalanceOf(snapshotCache, NativeContract.Governance.GasTokenId, UInt160.Zero);
        await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, balance, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, 8, assertOwner: false, callOnPayment: false);

        TransactionVerificationContext verificationContext = new();
        var tx = CreateTransactionWithFee(1, 2);
        var conflicts = new List<Transaction>();
        Assert.IsTrue(verificationContext.CheckTransaction(tx, conflicts, engine.SnapshotCache));
        verificationContext.AddTransaction(tx);
        Assert.IsTrue(verificationContext.CheckTransaction(tx, conflicts, engine.SnapshotCache));
        verificationContext.AddTransaction(tx);
        Assert.IsFalse(verificationContext.CheckTransaction(tx, conflicts, engine.SnapshotCache));
        verificationContext.RemoveTransaction(tx);
        Assert.IsTrue(verificationContext.CheckTransaction(tx, conflicts, engine.SnapshotCache));
        verificationContext.AddTransaction(tx);
        Assert.IsFalse(verificationContext.CheckTransaction(tx, conflicts, engine.SnapshotCache));
    }

    [TestMethod]
    public async Task TestTransactionSenderFeeWithConflicts()
    {
        // GasToken uses TokenManagement
        var snapshotCache = TestBlockchain.GetTestSnapshotCache();
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshotCache.Contains(tokenStateKey))
        {
            var tokenState = new TokenState
            {
                Type = TokenType.Fungible,
                Owner = NativeContract.Governance.Hash,
                Name = Governance.GasTokenName,
                Symbol = Governance.GasTokenSymbol,
                Decimals = Governance.GasTokenDecimals,
                TotalSupply = BigInteger.Zero,
                MaxSupply = BigInteger.MinusOne
            };
            snapshotCache.Add(tokenStateKey, new StorageItem(tokenState));
        }

        ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshotCache, settings: TestProtocolSettings.Default, gas: long.MaxValue);
        engine.LoadScript(Array.Empty<byte>());
        BigInteger balance = NativeContract.TokenManagement.BalanceOf(snapshotCache, NativeContract.Governance.GasTokenId, UInt160.Zero);
        await NativeContract.TokenManagement.BurnInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, balance, assertOwner: false);
        await NativeContract.TokenManagement.MintInternal(engine, NativeContract.Governance.GasTokenId, UInt160.Zero, 3 + 3 + 1, assertOwner: false, callOnPayment: false); // balance is enough for 2 transactions and 1 GAS is left.

        TransactionVerificationContext verificationContext = new();
        var tx = CreateTransactionWithFee(1, 2);
        var conflictingTx = CreateTransactionWithFee(1, 1); // costs 2 GAS

        var conflicts = new List<Transaction>();
        Assert.IsTrue(verificationContext.CheckTransaction(tx, conflicts, engine.SnapshotCache));
        verificationContext.AddTransaction(tx);
        Assert.IsTrue(verificationContext.CheckTransaction(tx, conflicts, engine.SnapshotCache));
        verificationContext.AddTransaction(tx);
        Assert.IsFalse(verificationContext.CheckTransaction(tx, conflicts, engine.SnapshotCache));

        conflicts.Add(conflictingTx);
        Assert.IsTrue(verificationContext.CheckTransaction(tx, conflicts, engine.SnapshotCache)); // 1 GAS is left on the balance + 2 GAS is free after conflicts removal => enough for one more trasnaction.
    }
}
