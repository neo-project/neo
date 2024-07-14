// Copyright (C) 2015-2024 The Neo Project.
//
// ConsensusContext.MakePayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Consensus
{
    partial class ConsensusContext
    {
        public ExtensiblePayload MakeChangeView(ChangeViewReason reason)
        {
            return ChangeViewPayloads[MyIndex] = MakeSignedPayload(new ChangeView
            {
                Reason = reason,
                Timestamp = TimeProvider.Current.UtcNow.ToTimestampMS()
            });
        }

        public ExtensiblePayload MakeCommit(uint pId)
        {
            return CommitPayloads[pId][MyIndex] ?? (CommitPayloads[pId][MyIndex] = MakeSignedPayload(new Commit
            {
                Signature = EnsureHeader(pId).Sign(keyPair, neoSystem.Settings.Network),
                PId = pId
            }));
        }

        private ExtensiblePayload MakeSignedPayload(ConsensusMessage message)
        {
            message.BlockIndex = Block[0].Index;
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
                wallet.Sign(sc);
            }
            catch (InvalidOperationException exception)
            {
                Utility.Log(nameof(ConsensusContext), LogLevel.Debug, exception.ToString());
                return;
            }
            payload.Witness = sc.GetWitnesses()[0];
        }

        /// <summary>
        /// Prevent that block exceed the max size
        /// </summary>
        /// <param name="txs">Ordered transactions</param>
        internal void EnsureMaxBlockLimitation(IEnumerable<Transaction> txs, uint pId)
        {
            uint maxTransactionsPerBlock = neoSystem.Settings.MaxTransactionsPerBlock;

            // Limit Speaker proposal to the limit `MaxTransactionsPerBlock` or all available transactions of the mempool
            txs = txs.Take((int)maxTransactionsPerBlock);

            List<UInt256> hashes = new();
            Transactions[pId] = new Dictionary<UInt256, Transaction>();
            VerificationContext[pId] = new TransactionVerificationContext();

            // Expected block size
            var blockSize = GetExpectedBlockSizeWithoutTransactions(txs.Count());
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
                Transactions[pId].Add(tx.Hash, tx);
                VerificationContext[pId].AddTransaction(tx);
            }

            TransactionHashes[pId] = hashes.ToArray();
        }

        internal IEnumerable<Transaction> PickTransactions()
        {
            var verifiedTxes = neoSystem.MemPool.GetSortedVerifiedTransactions();
            if (ViewNumber > 0 && LastProposal.Length > 0)
            {
                var txes = verifiedTxes.Where(p => LastProposal.Contains(p.Hash));
                if (txes.Count() > LastProposal.Length / 2)
                    return txes;
            }
            return verifiedTxes;
        }

        public ExtensiblePayload MakePrepareRequest(uint pId)
        {
            EnsureMaxBlockLimitation(PickTransactions(), pId);
            Block[pId].Header.Timestamp = Math.Max(TimeProvider.Current.UtcNow.ToTimestampMS(), PrevHeader.Timestamp + 1);
            Block[pId].Header.Nonce = GetNonce();

            return PreparationPayloads[pId][MyIndex] = MakeSignedPayload(new PrepareRequest
            {
                Version = Block[pId].Version,
                PrevHash = Block[pId].PrevHash,
                Timestamp = Block[pId].Timestamp,
                Nonce = Block[pId].Nonce,
                TransactionHashes = TransactionHashes[pId]
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
            uint pId = TransactionHashes[0] != null ? 0u : (TransactionHashes[1] != null ? 1u : 0u);
            if (TransactionHashes[pId] != null)
            {
                prepareRequestMessage = new PrepareRequest
                {
                    Version = Block[pId].Version,
                    PrevHash = Block[pId].PrevHash,
                    ViewNumber = ViewNumber,
                    Timestamp = Block[pId].Timestamp,
                    Nonce = Block[pId].Nonce,
                    BlockIndex = Block[pId].Index,
                    ValidatorIndex = Block[pId].PrimaryIndex,
                    TransactionHashes = TransactionHashes[pId]
                };
            }
            return MakeSignedPayload(new RecoveryMessage
            {
                PId = pId,
                ChangeViewMessages = LastChangeViewPayloads.Where(p => p != null).Select(p => GetChangeViewPayloadCompact(p)).Take(M).ToDictionary(p => p.ValidatorIndex),
                PrepareRequestMessage = prepareRequestMessage,
                // We only need a PreparationHash set if we don't have the PrepareRequest information.
                PreparationHash = TransactionHashes[pId] == null ? PreparationPayloads[pId].Where(p => p != null).GroupBy(p => GetMessage<PrepareResponse>(p).PreparationHash, (k, g) => new { Hash = k, Count = g.Count() }).OrderByDescending(p => p.Count).Select(p => p.Hash).FirstOrDefault() : null,
                PreparationMessages = PreparationPayloads[pId].Where(p => p != null).Select(p => GetPreparationPayloadCompact(p)).ToDictionary(p => p.ValidatorIndex),
                PreCommitMessages = PreCommitPayloads[pId].Where(p => p != null).Select(p => GetPreCommitPayloadCompact(p)).ToDictionary(p => p.ValidatorIndex),
                CommitMessages = CommitSent
                    ? CommitPayloads[pId].Where(p => p != null).Select(p => GetCommitPayloadCompact(p)).ToDictionary(p => p.ValidatorIndex)
                    : new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>()
            });
        }

        public ExtensiblePayload MakePrepareResponse(uint pId)
        {
            return PreparationPayloads[pId][MyIndex] = MakeSignedPayload(new PrepareResponse
            {
                PreparationHash = PreparationPayloads[pId][Block[pId].PrimaryIndex].Hash,
                PId = pId
            });
        }

        public ExtensiblePayload MakePreCommit(uint pId)
        {
            return PreCommitPayloads[pId][MyIndex] = MakeSignedPayload(new PreCommit
            {
                PreparationHash = PreparationPayloads[pId][Block[pId].PrimaryIndex].Hash,
                PId = pId
            });
        }

        private static ulong GetNonce()
        {
            Random _random = new();
            Span<byte> buffer = stackalloc byte[8];
            _random.NextBytes(buffer);
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }
    }
}
