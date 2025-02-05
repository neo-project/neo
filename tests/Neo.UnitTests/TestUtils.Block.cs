// Copyright (C) 2015-2025 The Neo Project.
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

namespace Neo.UnitTests
{
    public partial class TestUtils
    {
        const byte Prefix_Block = 5;
        const byte Prefix_BlockHash = 9;
        const byte Prefix_Transaction = 11;
        const byte Prefix_CurrentBlock = 12;

        /// <summary>
        /// Test Util function MakeHeader
        /// </summary>
        /// <param name="snapshot">The snapshot of the current storage provider. Can be null.</param>
        /// <param name="prevHash">The previous block hash</param>
        public static Header MakeHeader(DataCache snapshot, UInt256 prevHash)
        {
            return new Header
            {
                PrevHash = prevHash,
                MerkleRoot = UInt256.Parse("0x6226416a0e5aca42b5566f5a19ab467692688ba9d47986f6981a7f747bba2772"),
                Timestamp = new DateTime(2024, 06, 05, 0, 33, 1, 001, DateTimeKind.Utc).ToTimestampMS(),
                Index = snapshot != null ? NativeContract.Ledger.CurrentIndex(snapshot) + 1 : 0,
                Nonce = 0,
                NextConsensus = UInt160.Zero,
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };
        }

        public static Block MakeBlock(DataCache snapshot, UInt256 prevHash, int numberOfTransactions)
        {
            var block = new Block();
            var header = MakeHeader(snapshot, prevHash);
            Transaction[] transactions = new Transaction[numberOfTransactions];
            if (numberOfTransactions > 0)
            {
                for (int i = 0; i < numberOfTransactions; i++)
                {
                    transactions[i] = GetTransaction(UInt160.Zero);
                }
            }

            block.Header = header;
            block.Transactions = transactions;
            header.MerkleRoot = MerkleTree.ComputeRoot(block.Transactions.Select(p => p.Hash).ToArray());
            return block;
        }

        public static Block CreateBlockWithValidTransactions(DataCache snapshot,
            NEP6Wallet wallet, WalletAccount account, int numberOfTransactions)
        {
            var transactions = new List<Transaction>();
            for (var i = 0; i < numberOfTransactions; i++)
            {
                transactions.Add(CreateValidTx(snapshot, wallet, account));
            }

            return CreateBlockWithValidTransactions(snapshot, account, transactions.ToArray());
        }

        public static Block CreateBlockWithValidTransactions(DataCache snapshot,
            WalletAccount account, Transaction[] transactions)
        {
            var block = new Block();
            var key = NativeContract.Ledger.CreateStorageKey(Prefix_CurrentBlock);
            var state = snapshot.TryGet(key).GetInteroperable<HashIndexState>();
            var header = MakeHeader(snapshot, state.Hash);

            block.Header = header;
            block.Transactions = transactions;

            header.MerkleRoot = MerkleTree.ComputeRoot(block.Transactions.Select(p => p.Hash).ToArray());
            var contract = Contract.CreateMultiSigContract(1, TestProtocolSettings.SoleNode.StandbyCommittee);
            var sc = new ContractParametersContext(snapshot, header, TestProtocolSettings.SoleNode.Network);
            var signature = header.Sign(account.GetKey(), TestProtocolSettings.SoleNode.Network);
            sc.AddSignature(contract, TestProtocolSettings.SoleNode.StandbyCommittee[0], signature.ToArray());
            block.Header.Witness = sc.GetWitnesses()[0];

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
                var key = NativeContract.Ledger.CreateStorageKey(Prefix_Transaction, tx.Transaction.Hash);
                snapshot.Add(key, new StorageItem(tx));
            }
        }

        public static void BlocksAdd(DataCache snapshot, UInt256 hash, TrimmedBlock block)
        {
            var indexKey = NativeContract.Ledger.CreateStorageKey(Prefix_BlockHash, block.Index);
            snapshot.Add(indexKey, new StorageItem(hash.ToArray()));

            var hashKey = NativeContract.Ledger.CreateStorageKey(Prefix_Block, hash);
            snapshot.Add(hashKey, new StorageItem(block.ToArray()));

            var key = NativeContract.Ledger.CreateStorageKey(Prefix_CurrentBlock);
            var state = snapshot.GetAndChange(key, () => new(new HashIndexState())).GetInteroperable<HashIndexState>();
            state.Hash = hash;
            state.Index = block.Index;
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

            var indexKey = NativeContract.Ledger.CreateStorageKey(Prefix_BlockHash, block.Index);
            snapshot.Add(indexKey, new StorageItem(hash.ToArray()));

            var hashKey = NativeContract.Ledger.CreateStorageKey(Prefix_Block, hash);
            snapshot.Add(hashKey, new StorageItem(block.ToTrimmedBlock().ToArray()));

            var key = NativeContract.Ledger.CreateStorageKey(Prefix_CurrentBlock);
            var state = snapshot.GetAndChange(key, () => new(new HashIndexState())).GetInteroperable<HashIndexState>();
            state.Hash = hash;
            state.Index = block.Index;
        }

        public static string CreateInvalidBlockFormat()
        {
            // Create a valid block
            var validBlock = new Block
            {
                Header = new Header
                {
                    Version = 0,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Timestamp = 0,
                    Index = 0,
                    NextConsensus = UInt160.Zero,
                    Witness = new Witness
                    {
                        InvocationScript = ReadOnlyMemory<byte>.Empty,
                        VerificationScript = ReadOnlyMemory<byte>.Empty
                    }
                },
                Transactions = []
            };

            // Serialize the valid block
            byte[] validBlockBytes = validBlock.ToArray();

            // Corrupt the serialized data
            // For example, we can truncate the data by removing the last few bytes
            byte[] invalidBlockBytes = new byte[validBlockBytes.Length - 5];
            Array.Copy(validBlockBytes, invalidBlockBytes, invalidBlockBytes.Length);

            // Convert the corrupted data to a Base64 string
            return Convert.ToBase64String(invalidBlockBytes);
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
}
