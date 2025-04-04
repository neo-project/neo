// Copyright (C) 2015-2025 The Neo Project.
//
// ConsensusContext.MakePayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Extensions;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM.Types;
using Serilog;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace Neo.Plugins.DBFTPlugin.Consensus
{
    partial class ConsensusContext
    {
        private static readonly ILogger _log = Log.ForContext<ConsensusContext>();

        public ExtensiblePayload MakeChangeView(ChangeViewReason reason)
        {
            return ChangeViewPayloads[MyIndex] = MakeSignedPayload(new ChangeView
            {
                Reason = reason,
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS()
            });
        }

        public ExtensiblePayload MakeCommit()
        {
            if (CommitPayloads[MyIndex] is not null)
                return CommitPayloads[MyIndex];

            var signData = EnsureHeader().GetSignData(dbftSettings.Network);
            CommitPayloads[MyIndex] = MakeSignedPayload(new Commit
            {
                Signature = _signer.Sign(signData, _myPublicKey)
            });
            return CommitPayloads[MyIndex];
        }

        private ulong GetNonce()
        {
            byte[] nonce = new byte[sizeof(ulong)];
            try
            {
                RandomNumberGenerator.Fill(nonce);
            }
            catch (InvalidOperationException exception)
            {
                _log.Debug(exception, "Exception during RandomNumberGenerator.Fill");
                return 0;
            }
            return BinaryPrimitives.ReadUInt64LittleEndian(nonce);
        }

        private Block PrepareBlock()
        {
            var header = Block.Header;
            var block = Block;
            var snapshot = Snapshot;
            var settings = neoSystem.Settings;
            var memPool = neoSystem.MemPool;

            var policy = NativeContract.Policy;
            var maxBlockSize = dbftSettings.MaxBlockSize;
            var maxBlockSystemFee = dbftSettings.MaxBlockSystemFee;
            var feePerByte = policy.GetFeePerByte(snapshot);
            var txHashes = new List<UInt256>();
            long blockSize = block.Header.Size;
            long blockSystemFee = 0;

            Transactions = new Dictionary<UInt256, Transaction>();

            foreach (var tx in memPool.GetSortedVerifiedTransactions().Reverse())
            {
                long currentTxFee = tx.SystemFee + tx.NetworkFee;
                if (blockSystemFee + currentTxFee > maxBlockSystemFee) continue;
                if (blockSize + tx.Size > maxBlockSize) continue;
                if (NativeContract.Ledger.ContainsTransaction(snapshot, tx.Hash))
                    continue;
                if (NativeContract.Ledger.ContainsConflictHash(snapshot, tx.Hash, tx.Signers.Select(s => s.Account), settings.MaxTraceableBlocks))
                    continue;

                blockSize += tx.Size;
                blockSystemFee += tx.SystemFee;
                txHashes.Add(tx.Hash);
                Transactions.Add(tx.Hash, tx);
            }

            header.Timestamp = Math.Max(TimeProvider.Current.UtcNow.ToTimestampMS(), PrevHeader.Timestamp + 1);
            header.Nonce = GetNonce();
            block.Transactions = txHashes.Select(hash => Transactions[hash]).ToArray();
            header.MerkleRoot = MerkleTree.ComputeRoot(block.Transactions.Select(tx => tx.Hash).ToArray());

            _log.Information("Primary prepared block {BlockIndex} with {TxCount} transactions.", block.Index, block.Transactions.Length);

            return block;
        }

        private ExtensiblePayload MakeSignedPayload(ConsensusMessage message)
        {
            message.BlockIndex = Block.Index;
            message.ValidatorIndex = (byte)MyIndex;
            message.ViewNumber = ViewNumber;
            ExtensiblePayload payload = CreatePayload(message, null);
            SignPayload(payload);
            return payload;
        }

        private void SignPayload(ExtensiblePayload payload)
        {
            ContractParametersContext sc;
            try
            {
                sc = new ContractParametersContext(neoSystem.StoreView, payload, dbftSettings.Network);
                _signer.Sign(sc);
            }
            catch (InvalidOperationException exception)
            {
                _log.Debug(exception, "Failed to sign payload");
                return;
            }
            payload.Witness = sc.GetWitnesses()[0];
        }

        /// <summary>
        /// Prevent that block exceed the max size
        /// </summary>
        /// <param name="txs">Ordered transactions</param>
        internal void EnsureMaxBlockLimitation(Transaction[] txs)
        {
            var hashes = new List<UInt256>();
            Transactions = new Dictionary<UInt256, Transaction>();
            VerificationContext = new TransactionVerificationContext();

            // Expected block size
            var blockSize = GetExpectedBlockSizeWithoutTransactions(txs.Length);
            var blockSystemFee = 0L;

            // Iterate transaction until reach the size or maximum system fee
            foreach (Transaction tx in txs)
            {
                // Check if maximum block size has been already exceeded with the current selected set
                blockSize += tx.Size;
                if (blockSize > dbftSettings.MaxBlockSize) break;

                // Check if maximum block system fee has been already exceeded with the current selected set
                blockSystemFee += tx.SystemFee;
                if (blockSystemFee > dbftSettings.MaxBlockSystemFee) break;

                hashes.Add(tx.Hash);
                Transactions.Add(tx.Hash, tx);
                VerificationContext.AddTransaction(tx);
            }

            TransactionHashes = hashes.ToArray();
        }

        public ExtensiblePayload MakePrepareRequest()
        {
            var maxTransactionsPerBlock = neoSystem.Settings.MaxTransactionsPerBlock;
            // Limit Speaker proposal to the limit `MaxTransactionsPerBlock` or all available transactions of the mempool
            EnsureMaxBlockLimitation(neoSystem.MemPool.GetSortedVerifiedTransactions((int)maxTransactionsPerBlock));
            Block.Header.Timestamp = Math.Max(TimeProvider.Current.UtcNow.ToTimestampMS(), PrevHeader.Timestamp + 1);
            Block.Header.Nonce = GetNonce();
            return PreparationPayloads[MyIndex] = MakeSignedPayload(new PrepareRequest
            {
                Version = Block.Version,
                PrevHash = Block.PrevHash,
                Timestamp = Block.Timestamp,
                Nonce = Block.Nonce,
                TransactionHashes = TransactionHashes
            });
        }

        public ExtensiblePayload MakeRecoveryRequest()
        {
            return MakeSignedPayload(new RecoveryRequest
            {
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS()
            });
        }

        public ExtensiblePayload MakeRecoveryMessage()
        {
            PrepareRequest prepareRequestMessage = null;
            if (TransactionHashes != null)
            {
                prepareRequestMessage = new PrepareRequest
                {
                    Version = Block.Version,
                    PrevHash = Block.PrevHash,
                    ViewNumber = ViewNumber,
                    Timestamp = Block.Timestamp,
                    Nonce = Block.Nonce,
                    BlockIndex = Block.Index,
                    ValidatorIndex = Block.PrimaryIndex,
                    TransactionHashes = TransactionHashes
                };
            }
            return MakeSignedPayload(new RecoveryMessage
            {
                ChangeViewMessages = LastChangeViewPayloads.Where(p => p != null)
                    .Select(p => GetChangeViewPayloadCompact(p))
                    .Take(M)
                    .ToDictionary(p => p.ValidatorIndex),
                PrepareRequestMessage = prepareRequestMessage,
                // We only need a PreparationHash set if we don't have the PrepareRequest information.
                PreparationHash = TransactionHashes == null
                    ? PreparationPayloads.Where(p => p != null)
                        .GroupBy(p => GetMessage<PrepareResponse>(p).PreparationHash, (k, g) => new { Hash = k, Count = g.Count() })
                        .OrderByDescending(p => p.Count)
                        .Select(p => p.Hash)
                        .FirstOrDefault()
                    : null,
                PreparationMessages = PreparationPayloads.Where(p => p != null)
                    .Select(p => GetPreparationPayloadCompact(p))
                    .ToDictionary(p => p.ValidatorIndex),
                CommitMessages = CommitSent
                    ? CommitPayloads.Where(p => p != null).Select(p => GetCommitPayloadCompact(p)).ToDictionary(p => p.ValidatorIndex)
                    : new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>()
            });
        }

        public ExtensiblePayload MakePrepareResponse()
        {
            return PreparationPayloads[MyIndex] = MakeSignedPayload(new PrepareResponse
            {
                PreparationHash = PreparationPayloads[Block.PrimaryIndex].Hash
            });
        }
    }
}
