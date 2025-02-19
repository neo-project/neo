// Copyright (C) 2015-2025 The Neo Project.
//
// LedgerContract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract for storing all blocks and transactions.
    /// </summary>
    public sealed class LedgerContract : NativeContract
    {
        private const byte Prefix_BlockHash = 9;
        private const byte Prefix_CurrentBlock = 12;
        private const byte Prefix_Block = 5;
        private const byte Prefix_Transaction = 11;

        private readonly StorageKey _currentBlock;

        internal LedgerContract() : base()
        {
            _currentBlock = CreateStorageKey(Prefix_CurrentBlock);
        }

        internal override ContractTask OnPersistAsync(ApplicationEngine engine)
        {
            TransactionState[] transactions = engine.PersistingBlock.Transactions.Select(p => new TransactionState
            {
                BlockIndex = engine.PersistingBlock.Index,
                Transaction = p,
                State = VMState.NONE
            }).ToArray();
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_BlockHash, engine.PersistingBlock.Index), new StorageItem(engine.PersistingBlock.Hash.ToArray()));
            engine.SnapshotCache.Add(CreateStorageKey(Prefix_Block, engine.PersistingBlock.Hash), new StorageItem(Trim(engine.PersistingBlock).ToArray()));
            foreach (TransactionState tx in transactions)
            {
                // It's possible that there are previously saved malicious conflict records for this transaction.
                // If so, then remove it and store the relevant transaction itself.
                engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Transaction, tx.Transaction.Hash), () => new StorageItem(new TransactionState())).FromReplica(new StorageItem(tx));

                // Store transaction's conflicits.
                var conflictingSigners = tx.Transaction.Signers.Select(s => s.Account);
                foreach (var attr in tx.Transaction.GetAttributes<Conflicts>())
                {
                    engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Transaction, attr.Hash), () => new StorageItem(new TransactionState())).FromReplica(new StorageItem(new TransactionState() { BlockIndex = engine.PersistingBlock.Index }));
                    foreach (var signer in conflictingSigners)
                    {
                        engine.SnapshotCache.GetAndChange(CreateStorageKey(Prefix_Transaction, attr.Hash, signer), () => new StorageItem(new TransactionState())).FromReplica(new StorageItem(new TransactionState() { BlockIndex = engine.PersistingBlock.Index }));
                    }
                }
            }

            engine.SetState(transactions);
            return ContractTask.CompletedTask;
        }

        internal override ContractTask PostPersistAsync(ApplicationEngine engine)
        {
            HashIndexState state = engine.SnapshotCache.GetAndChange(_currentBlock, () => new StorageItem(new HashIndexState())).GetInteroperable<HashIndexState>();
            state.Hash = engine.PersistingBlock.Hash;
            state.Index = engine.PersistingBlock.Index;
            return ContractTask.CompletedTask;
        }

        internal bool Initialized(DataCache snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            return snapshot.Find(CreateStorageKey(Prefix_Block).ToArray()).Any();
        }

        private bool IsTraceableBlock(IReadOnlyStore snapshot, uint index, uint maxTraceableBlocks)
        {
            uint currentIndex = CurrentIndex(snapshot);
            if (index > currentIndex) return false;
            return index + maxTraceableBlocks > currentIndex;
        }

        /// <summary>
        /// Gets the hash of the specified block.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="index">The index of the block.</param>
        /// <returns>The hash of the block.</returns>
        public UInt256 GetBlockHash(IReadOnlyStore snapshot, uint index)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            var key = CreateStorageKey(Prefix_BlockHash, index);
            return snapshot.TryGet(key, out var item) ? new UInt256(item.Value.Span) : null;
        }

        /// <summary>
        /// Gets the hash of the current block.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The hash of the current block.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public UInt256 CurrentHash(IReadOnlyStore snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            return snapshot[_currentBlock].GetInteroperable<HashIndexState>().Hash;
        }

        /// <summary>
        /// Gets the index of the current block.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <returns>The index of the current block.</returns>
        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        public uint CurrentIndex(IReadOnlyStore snapshot)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            return snapshot[_currentBlock].GetInteroperable<HashIndexState>().Index;
        }

        /// <summary>
        /// Determine whether the specified block is contained in the blockchain.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the block.</param>
        /// <returns>
        /// <see langword="true"/> if the blockchain contains the block; otherwise, <see langword="false"/>.
        /// </returns>
        public bool ContainsBlock(IReadOnlyStore snapshot, UInt256 hash)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            return snapshot.Contains(CreateStorageKey(Prefix_Block, hash));
        }

        /// <summary>
        /// Determine whether the specified transaction is contained in the blockchain.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the transaction.</param>
        /// <returns>
        /// <see langword="true"/> if the blockchain contains the transaction; otherwise, <see langword="false"/>.
        /// </returns>
        public bool ContainsTransaction(IReadOnlyStore snapshot, UInt256 hash)
        {
            var txState = GetTransactionState(snapshot, hash);
            return txState != null;
        }

        /// <summary>
        /// Determine whether the specified transaction hash is contained in the blockchain
        /// as the hash of conflicting transaction.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the conflicting transaction.</param>
        /// <param name="signers">The list of signer accounts of the conflicting transaction.</param>
        /// <param name="maxTraceableBlocks">MaxTraceableBlocks protocol setting.</param>
        /// <returns>
        /// <see langword="true"/> if the blockchain contains the hash of the conflicting transaction;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool ContainsConflictHash(IReadOnlyStore snapshot, UInt256 hash, IEnumerable<UInt160> signers, uint maxTraceableBlocks)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            if (signers is null)
                throw new ArgumentNullException(nameof(signers));

            // Check the dummy stub firstly to define whether there's exist at least one conflict record.
            var key = CreateStorageKey(Prefix_Transaction, hash);
            var stub = snapshot.TryGet(key, out var item) ? item.GetInteroperable<TransactionState>() : null;
            if (stub is null || stub.Transaction is not null || !IsTraceableBlock(snapshot, stub.BlockIndex, maxTraceableBlocks))
                return false;

            // At least one conflict record is found, then need to check signers intersection.
            foreach (var signer in signers)
            {
                key = CreateStorageKey(Prefix_Transaction, hash, signer);
                var state = snapshot.TryGet(key, out var tx) ? tx.GetInteroperable<TransactionState>() : null;
                if (state is not null && IsTraceableBlock(snapshot, state.BlockIndex, maxTraceableBlocks))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a <see cref="TrimmedBlock"/> with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the block.</param>
        /// <returns>The trimmed block.</returns>
        public TrimmedBlock GetTrimmedBlock(IReadOnlyStore snapshot, UInt256 hash)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            var key = CreateStorageKey(Prefix_Block, hash);
            if (snapshot.TryGet(key, out var item))
                return item.Value.AsSerializable<TrimmedBlock>();
            return null;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private TrimmedBlock GetBlock(ApplicationEngine engine, byte[] indexOrHash)
        {
            UInt256 hash;
            if (indexOrHash.Length < UInt256.Length)
                hash = GetBlockHash(engine.SnapshotCache, (uint)new BigInteger(indexOrHash));
            else if (indexOrHash.Length == UInt256.Length)
                hash = new UInt256(indexOrHash);
            else
                throw new ArgumentException(null, nameof(indexOrHash));
            if (hash is null) return null;
            TrimmedBlock block = GetTrimmedBlock(engine.SnapshotCache, hash);
            if (block is null || !IsTraceableBlock(engine.SnapshotCache, block.Index, engine.ProtocolSettings.MaxTraceableBlocks)) return null;
            return block;
        }

        /// <summary>
        /// Gets a block with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the block.</param>
        /// <returns>The block with the specified hash.</returns>
        public Block GetBlock(IReadOnlyStore snapshot, UInt256 hash)
        {
            TrimmedBlock state = GetTrimmedBlock(snapshot, hash);
            if (state is null) return null;
            return new Block
            {
                Header = state.Header,
                Transactions = state.Hashes.Select(p => GetTransaction(snapshot, p)).ToArray()
            };
        }

        /// <summary>
        /// Gets a block with the specified index.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="index">The index of the block.</param>
        /// <returns>The block with the specified index.</returns>
        public Block GetBlock(IReadOnlyStore snapshot, uint index)
        {
            UInt256 hash = GetBlockHash(snapshot, index);
            if (hash is null) return null;
            return GetBlock(snapshot, hash);
        }

        /// <summary>
        /// Gets a block header with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the block.</param>
        /// <returns>The block header with the specified hash.</returns>
        public Header GetHeader(IReadOnlyStore snapshot, UInt256 hash)
        {
            return GetTrimmedBlock(snapshot, hash)?.Header;
        }

        /// <summary>
        /// Gets a block header with the specified index.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="index">The index of the block.</param>
        /// <returns>The block header with the specified index.</returns>
        public Header GetHeader(DataCache snapshot, uint index)
        {
            UInt256 hash = GetBlockHash(snapshot, index);
            if (hash is null) return null;
            return GetHeader(snapshot, hash);
        }

        /// <summary>
        /// Gets a <see cref="TransactionState"/> with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the transaction.</param>
        /// <returns>The <see cref="TransactionState"/> with the specified hash.</returns>
        public TransactionState GetTransactionState(IReadOnlyStore snapshot, UInt256 hash)
        {
            if (snapshot is null)
                throw new ArgumentNullException(nameof(snapshot));

            var key = CreateStorageKey(Prefix_Transaction, hash);
            var state = snapshot.TryGet(key, out var item) ? item.GetInteroperable<TransactionState>() : null;
            return state?.Transaction is null ? null : state;
        }

        /// <summary>
        /// Gets a transaction with the specified hash.
        /// </summary>
        /// <param name="snapshot">The snapshot used to read data.</param>
        /// <param name="hash">The hash of the transaction.</param>
        /// <returns>The transaction with the specified hash.</returns>
        public Transaction GetTransaction(IReadOnlyStore snapshot, UInt256 hash)
        {
            return GetTransactionState(snapshot, hash)?.Transaction;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates, Name = "getTransaction")]
        private Transaction GetTransactionForContract(ApplicationEngine engine, UInt256 hash)
        {
            TransactionState state = GetTransactionState(engine.SnapshotCache, hash);
            if (state is null || !IsTraceableBlock(engine.SnapshotCache, state.BlockIndex, engine.ProtocolSettings.MaxTraceableBlocks)) return null;
            return state.Transaction;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private Signer[] GetTransactionSigners(ApplicationEngine engine, UInt256 hash)
        {
            TransactionState state = GetTransactionState(engine.SnapshotCache, hash);
            if (state is null || !IsTraceableBlock(engine.SnapshotCache, state.BlockIndex, engine.ProtocolSettings.MaxTraceableBlocks)) return null;
            return state.Transaction.Signers;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private VMState GetTransactionVMState(ApplicationEngine engine, UInt256 hash)
        {
            TransactionState state = GetTransactionState(engine.SnapshotCache, hash);
            if (state is null || !IsTraceableBlock(engine.SnapshotCache, state.BlockIndex, engine.ProtocolSettings.MaxTraceableBlocks)) return VMState.NONE;
            return state.State;
        }

        [ContractMethod(CpuFee = 1 << 15, RequiredCallFlags = CallFlags.ReadStates)]
        private int GetTransactionHeight(ApplicationEngine engine, UInt256 hash)
        {
            TransactionState state = GetTransactionState(engine.SnapshotCache, hash);
            if (state is null || !IsTraceableBlock(engine.SnapshotCache, state.BlockIndex, engine.ProtocolSettings.MaxTraceableBlocks)) return -1;
            return (int)state.BlockIndex;
        }

        [ContractMethod(CpuFee = 1 << 16, RequiredCallFlags = CallFlags.ReadStates)]
        private Transaction GetTransactionFromBlock(ApplicationEngine engine, byte[] blockIndexOrHash, int txIndex)
        {
            UInt256 hash;
            if (blockIndexOrHash.Length < UInt256.Length)
                hash = GetBlockHash(engine.SnapshotCache, (uint)new BigInteger(blockIndexOrHash));
            else if (blockIndexOrHash.Length == UInt256.Length)
                hash = new UInt256(blockIndexOrHash);
            else
                throw new ArgumentException(null, nameof(blockIndexOrHash));
            if (hash is null) return null;
            TrimmedBlock block = GetTrimmedBlock(engine.SnapshotCache, hash);
            if (block is null || !IsTraceableBlock(engine.SnapshotCache, block.Index, engine.ProtocolSettings.MaxTraceableBlocks)) return null;
            if (txIndex < 0 || txIndex >= block.Hashes.Length)
                throw new ArgumentOutOfRangeException(nameof(txIndex));
            return GetTransaction(engine.SnapshotCache, block.Hashes[txIndex]);
        }

        private static TrimmedBlock Trim(Block block)
        {
            return new TrimmedBlock
            {
                Header = block.Header,
                Hashes = block.Transactions.Select(p => p.Hash).ToArray()
            };
        }
    }
}
