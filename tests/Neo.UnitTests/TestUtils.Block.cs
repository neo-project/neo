// Copyright (C) 2015-2024 The Neo Project.
//
// TestUtils.Block.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Util.Internal;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests;

public partial class TestUtils
{
    const byte Prefix_Block = 5;
    const byte Prefix_BlockHash = 9;
    const byte Prefix_Transaction = 11;

    /// <summary>
    /// Test Util function SetupHeaderWithValues
    /// </summary>
    /// <param name="header">The header to be assigned</param>
    /// <param name="val256">PrevHash</param>
    /// <param name="merkRootVal">MerkleRoot</param>
    /// <param name="val160">NextConsensus</param>
    /// <param name="timestampVal">Timestamp</param>
    /// <param name="indexVal">Index</param>
    /// <param name="nonceVal">Nonce</param>
    /// <param name="scriptVal">Witness</param>
    public static void SetupHeaderWithValues(Header header, UInt256 val256, out UInt256 merkRootVal, out UInt160 val160, out ulong timestampVal, out ulong nonceVal, out uint indexVal, out Witness scriptVal)
    {
        header.PrevHash = val256;
        header.MerkleRoot = merkRootVal = UInt256.Parse("0x6226416a0e5aca42b5566f5a19ab467692688ba9d47986f6981a7f747bba2772");
        header.Timestamp = timestampVal = new DateTime(1980, 06, 01, 0, 0, 1, 001, DateTimeKind.Utc).ToTimestampMS(); // GMT: Sunday, June 1, 1980 12:00:01.001 AM
        header.Index = indexVal = 0;
        header.Nonce = nonceVal = 0;
        header.NextConsensus = val160 = UInt160.Zero;
        header.Witness = scriptVal = new Witness
        {
            InvocationScript = Array.Empty<byte>(),
            VerificationScript = new[] { (byte)OpCode.PUSH1 }
        };
    }

    public static void SetupBlockWithValues(Block block, UInt256 val256, out UInt256 merkRootVal, out UInt160 val160, out ulong timestampVal, out ulong nonceVal, out uint indexVal, out Witness scriptVal, out Transaction[] transactionsVal, int numberOfTransactions)
    {
        Header header = new Header();
        SetupHeaderWithValues(header, val256, out merkRootVal, out val160, out timestampVal, out nonceVal, out indexVal, out scriptVal);

        transactionsVal = new Transaction[numberOfTransactions];
        if (numberOfTransactions > 0)
        {
            for (int i = 0; i < numberOfTransactions; i++)
            {
                transactionsVal[i] = GetTransaction(UInt160.Zero);
            }
        }

        block.Header = header;
        block.Transactions = transactionsVal;

        header.MerkleRoot = merkRootVal = MerkleTree.ComputeRoot(block.Transactions.Select(p => p.Hash).ToArray());
    }

    public static Block CreateBlockWithValidTransactions(DataCache snapshot, NEP6Wallet wallet, WalletAccount account, int numberOfTransactions)
    {
        var block = new Block();

        var transactions = new List<Transaction>();
        for (var i = 0; i < numberOfTransactions; i++)
        {
            transactions.Add(CreateValidTx(snapshot, wallet, account));
        }

        var header = new Header();
        SetupHeaderWithValues(header, RandomUInt256(), out _, out _, out _, out _, out _, out _);

        block.Header = header;
        block.Transactions = transactions.ToArray();

        header.MerkleRoot = MerkleTree.ComputeRoot(block.Transactions.Select(p => p.Hash).ToArray());
        return block;
    }

    public static void BlocksDelete(DataCache snapshot, UInt256 hash)
    {
        snapshot.Delete(NativeContract.Ledger.CreateStorageKey(Prefix_BlockHash, hash));
        snapshot.Delete(NativeContract.Ledger.CreateStorageKey(Prefix_Block, hash));
    }

    public static void TransactionAdd(DataCache snapshot, params TransactionState[] txs)
    {
        foreach (var tx in txs)
        {
            snapshot.Add(NativeContract.Ledger.CreateStorageKey(Prefix_Transaction, tx.Transaction.Hash), new StorageItem(tx));
        }
    }

    public static void BlocksAdd(DataCache snapshot, UInt256 hash, TrimmedBlock block)
    {
        snapshot.Add(NativeContract.Ledger.CreateStorageKey(Prefix_BlockHash, block.Index), new StorageItem(hash.ToArray()));
        snapshot.Add(NativeContract.Ledger.CreateStorageKey(Prefix_Block, hash), new StorageItem(block.ToArray()));
    }

    public static void BlocksAdd(DataCache snapshot, UInt256 hash, Block block)
    {

        block.Transactions.ForEach(tx =>
        {
            var state = new TransactionState
            {
                BlockIndex = block.Index,
                Transaction = tx
            };
            TransactionAdd(snapshot, state);
        });
        snapshot.Add(NativeContract.Ledger.CreateStorageKey(Prefix_BlockHash, block.Index), new StorageItem(hash.ToArray()));
        snapshot.Add(NativeContract.Ledger.CreateStorageKey(Prefix_Block, hash), new StorageItem(block.ToTrimmedBlock().ToArray()));
    }

    public static TrimmedBlock ToTrimmedBlock(this Block block)
    {
        return new TrimmedBlock
        {
            Header = block.Header,
            Hashes = block.Transactions.Select(p => p.Hash).ToArray()
        };
    }
}
