// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Blockchain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.TestKit;
using Akka.TestKit.MsTest;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System.Numerics;

namespace Neo.UnitTests.Ledger;

[TestClass]
public class UT_Blockchain : TestKit
{
    private NeoSystem _system = null!;
    private Transaction txSample = null!;
    private TestProbe senderProbe = null!;

    [TestInitialize]
    public void Initialize()
    {
        _system = TestBlockchain.GetSystem();
        senderProbe = CreateTestProbe();
        txSample = new Transaction
        {
            Attributes = [],
            Script = Array.Empty<byte>(),
            Signers = [new Signer { Account = UInt160.Zero }],
            Witnesses = []
        };
        _system.MemPool.TryAdd(txSample, _system.GetSnapshotCache());
    }

    [TestMethod]
    public void TestValidTransaction()
    {
        var snapshot = _system.GetSnapshotCache();
        var walletA = TestUtils.GenerateTestWallet("123");
        var acc = walletA.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshot.Contains(tokenStateKey))
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
            snapshot.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 100_000_000 * Governance.GasTokenFactor;
        snapshot.Commit();

        // Make transaction

        var tx = TestUtils.CreateValidTx(snapshot, walletA, acc.ScriptHash, 0);

        senderProbe.Send(_system.Blockchain, tx);
        senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.Succeed, cancellationToken: CancellationToken.None);

        senderProbe.Send(_system.Blockchain, tx);
        senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.AlreadyInPool, cancellationToken: CancellationToken.None);
    }

    [TestMethod]
    public void TestInvalidTransaction()
    {
        var snapshot = _system.GetSnapshotCache();
        var walletA = TestUtils.GenerateTestWallet("123");
        var acc = walletA.CreateAccount();

        // Fake balance - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshot.Contains(tokenStateKey))
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
            snapshot.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balance
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(acc.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 100_000_000 * Governance.GasTokenFactor;
        snapshot.Commit();

        // Make transaction

        var tx = TestUtils.CreateValidTx(snapshot, walletA, acc.ScriptHash, 0);
        tx.Signers = null!;

        senderProbe.Send(_system.Blockchain, tx);
        senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.Invalid, cancellationToken: CancellationToken.None);
    }

    internal static StorageKey CreateStorageKey(byte prefix, byte[]? key = null)
    {
        byte[] buffer = GC.AllocateUninitializedArray<byte>(sizeof(byte) + (key?.Length ?? 0));
        buffer[0] = prefix;
        key?.CopyTo(buffer.AsSpan(1));
        return new()
        {
            Id = NativeContract.NEO.Id,
            Key = buffer
        };
    }


    [TestMethod]
    public void TestMaliciousOnChainConflict()
    {
        var snapshot = _system.GetSnapshotCache();
        var walletA = TestUtils.GenerateTestWallet("123");
        var accA = walletA.CreateAccount();
        var walletB = TestUtils.GenerateTestWallet("456");
        var accB = walletB.CreateAccount();
        ApplicationEngine engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot, settings: _system.Settings, gas: long.MaxValue);
        engine.LoadScript(Array.Empty<byte>());

        // Fake balance for accounts A and B - GAS token uses TokenManagement with Prefix_AccountState = 12
        // First, create TokenState for GAS token (required by TokenManagement.BalanceOf)
        var tokenStateKey = new KeyBuilder(NativeContract.TokenManagement.Id, 10).Add(NativeContract.Governance.GasTokenId);
        if (!snapshot.Contains(tokenStateKey))
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
            snapshot.Add(tokenStateKey, new StorageItem(tokenState));
        }
        // Then set account balances
        var key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(accA.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 100_000_000 * Governance.GasTokenFactor;
        snapshot.Commit();

        key = new KeyBuilder(NativeContract.TokenManagement.Id, 12).Add(accB.ScriptHash).Add(NativeContract.Governance.GasTokenId);
        entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
        entry.GetInteroperable<AccountState>().Balance = 100_000_000 * Governance.GasTokenFactor;
        snapshot.Commit();

        // Create transactions:
        //    tx1 conflicts with tx2 and has the same sender (thus, it's a valid conflict and must prevent tx2 from entering the chain);
        //    tx2 conflicts with tx3 and has different sender (thus, this conflict is invalid and must not prevent tx3 from entering the chain).
        var tx1 = TestUtils.CreateValidTx(snapshot, walletA, accA.ScriptHash, 0);
        var tx2 = TestUtils.CreateValidTx(snapshot, walletA, accA.ScriptHash, 1);
        var tx3 = TestUtils.CreateValidTx(snapshot, walletB, accB.ScriptHash, 2);

        tx1.Attributes = [new Conflicts() { Hash = tx2.Hash }, new Conflicts() { Hash = tx3.Hash }];

        // Persist tx1.
        var block = new Block
        {
            Header = new Header()
            {
                Index = 5, // allow tx1, tx2 and tx3 to fit into MaxValidUntilBlockIncrement.
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                PrevHash = UInt256.Zero,
                Witness = Witness.Empty,
            },
            Transactions = [tx1],
        };
        byte[] onPersistScript;
        using (ScriptBuilder sb = new())
        {
            sb.EmitSysCall(ApplicationEngine.System_Contract_NativeOnPersist);
            onPersistScript = sb.ToArray();
        }
        using (ApplicationEngine engine2 = ApplicationEngine.Create(TriggerType.OnPersist, null, snapshot, block, _system.Settings, 0))
        {
            engine2.LoadScript(onPersistScript);
            if (engine2.Execute() != VMState.HALT) throw engine2.FaultException!;
            engine2.SnapshotCache.Commit();
        }
        snapshot.Commit();

        // Run PostPersist to update current block index in native Ledger.
        // Relevant current block index is needed for conflict records checks.
        byte[] postPersistScript;
        using (ScriptBuilder sb = new())
        {
            sb.EmitSysCall(ApplicationEngine.System_Contract_NativePostPersist);
            postPersistScript = sb.ToArray();
        }
        using (ApplicationEngine engine2 = ApplicationEngine.Create(TriggerType.PostPersist, null, snapshot, block, _system.Settings, 0))
        {
            engine2.LoadScript(postPersistScript);
            if (engine2.Execute() != VMState.HALT) throw engine2.FaultException!;
            engine2.SnapshotCache.Commit();
        }
        snapshot.Commit();

        // Add tx2: must fail because valid conflict is alredy on chain (tx1).
        senderProbe.Send(_system.Blockchain, tx2);
        senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.HasConflicts, cancellationToken: CancellationToken.None);

        // Add tx3: must succeed because on-chain conflict is invalid (doesn't have proper signer).
        senderProbe.Send(_system.Blockchain, tx3);
        senderProbe.ExpectMsg<Blockchain.RelayResult>(p => p.Result == VerifyResult.Succeed, cancellationToken: CancellationToken.None);
    }
}
