// Copyright (C) 2015-2025 The Neo Project.
//
// ConsensusService.OnMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Consensus
{
    partial class ConsensusService
    {
        private void OnConsensusPayload(ExtensiblePayload payload)
        {
            if (context.BlockSent) return;

            ConsensusMessage message;
            try
            {
                message = context.GetMessage(payload);
            }
            catch (FormatException ex)
            {
                _log.Warning(ex, "Failed to deserialize consensus message from {Sender}", Sender);
                return;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception deserializing consensus message from {Sender}", Sender);
                return;
            }

            if (message.ValidatorIndex >= context.Validators.Length)
            {
                _log.Warning("Received consensus message with invalid ValidatorIndex {ValidatorIndex} from {Sender}", message.ValidatorIndex, Sender);
                return;
            }

            if (message.BlockIndex == context.Block.Index)
            {
                if (!message.Verify(neoSystem.Settings)) return;
                if (payload.Sender != Contract.CreateSignatureRedeemScript(context.Validators[message.ValidatorIndex]).ToScriptHash()) return;
                context.LastSeenMessage[context.Validators[message.ValidatorIndex]] = message.BlockIndex;
                switch (message)
                {
                    case PrepareRequest request:
                        OnPrepareRequestReceived(payload, request);
                        break;
                    case PrepareResponse response:
                        OnPrepareResponseReceived(payload, response);
                        break;
                    case ChangeView view:
                        OnChangeViewReceived(payload, view);
                        break;
                    case Commit commit:
                        OnCommitReceived(payload, commit);
                        break;
                    case RecoveryRequest request:
                        OnRecoveryRequestReceived(payload, request);
                        break;
                    case RecoveryMessage recovery:
                        OnRecoveryMessageReceived(recovery);
                        break;
                }
            }
            else if (message.BlockIndex > context.Block.Index)
            {
                _log.Debug("Received consensus message for future block {BlockIndex} from {Sender}", message.BlockIndex, Sender);
                // handle future message
            }
            else
            {
                _log.Debug("Received consensus message for past block {BlockIndex} from {Sender}", message.BlockIndex, Sender);
                // handle past message
            }
        }

        private void OnPrepareRequestReceived(ExtensiblePayload payload, PrepareRequest message)
        {
            if (context.RequestSentOrReceived || context.NotAcceptingPayloadsDueToViewChanging) return;
            if (message.ValidatorIndex != context.Block.PrimaryIndex || message.ViewNumber != context.ViewNumber) return;
            if (message.Version != context.Block.Version || message.PrevHash != context.Block.PrevHash) return;
            if (message.TransactionHashes.Length > neoSystem.Settings.MaxTransactionsPerBlock) return;
            _log.Information("PrepareRequest received: Height={Height}, View={View}, SenderIndex={Index}, TxCount={TxCount}",
                message.BlockIndex, message.ViewNumber, message.ValidatorIndex, message.TransactionHashes.Length);
            if (message.Timestamp <= context.PrevHeader.Timestamp || message.Timestamp > TimeProvider.Current.UtcNow.AddMilliseconds(8 * neoSystem.Settings.MillisecondsPerBlock).ToTimestampMS())
            {
                _log.Warning("Timestamp incorrect: {Timestamp}", message.Timestamp);
                return;
            }

            if (message.TransactionHashes.Any(p => NativeContract.Ledger.ContainsTransaction(context.Snapshot, p)))
            {
                _log.Warning("Invalid request: transaction already exists");
                return;
            }

            // Timeout extension: prepare request has been received with success
            // around 2*15/M=30.0/5 ~ 40% block time (for M=5)
            ExtendTimerByFactor(2);

            context.Block.Header.Timestamp = message.Timestamp;
            context.Block.Header.Nonce = message.Nonce;
            context.TransactionHashes = message.TransactionHashes;

            context.Transactions = new Dictionary<UInt256, Transaction>();
            context.VerificationContext = new TransactionVerificationContext();
            for (int i = 0; i < context.PreparationPayloads.Length; i++)
                if (context.PreparationPayloads[i] != null)
                    if (!context.GetMessage<PrepareResponse>(context.PreparationPayloads[i]).PreparationHash.Equals(payload.Hash))
                        context.PreparationPayloads[i] = null;
            context.PreparationPayloads[message.ValidatorIndex] = payload;
            byte[] hashData = context.EnsureHeader().GetSignData(neoSystem.Settings.Network);
            for (int i = 0; i < context.CommitPayloads.Length; i++)
                if (context.GetMessage(context.CommitPayloads[i])?.ViewNumber == context.ViewNumber)
                    if (!Crypto.VerifySignature(hashData, context.GetMessage<Commit>(context.CommitPayloads[i]).Signature.Span, context.Validators[i]))
                        context.CommitPayloads[i] = null;

            if (context.TransactionHashes.Length == 0)
            {
                // There are no tx so we should act like if all the transactions were filled
                CheckPrepareResponse();
                return;
            }

            Dictionary<UInt256, Transaction> mempoolVerified = neoSystem.MemPool.GetVerifiedTransactions().ToDictionary(p => p.Hash);
            List<Transaction> unverified = new List<Transaction>();
            foreach (UInt256 hash in context.TransactionHashes)
            {
                if (mempoolVerified.TryGetValue(hash, out Transaction tx))
                {
                    if (NativeContract.Ledger.ContainsConflictHash(context.Snapshot, hash, tx.Signers.Select(s => s.Account), neoSystem.Settings.MaxTraceableBlocks))
                    {
                        _log.Warning("Invalid request: transaction has on-chain conflict");
                        return;
                    }

                    if (!AddTransaction(tx, false))
                        return;
                }
                else
                {
                    if (neoSystem.MemPool.TryGetValue(hash, out tx))
                    {
                        if (NativeContract.Ledger.ContainsConflictHash(context.Snapshot, hash, tx.Signers.Select(s => s.Account), neoSystem.Settings.MaxTraceableBlocks))
                        {
                            _log.Warning("Invalid request: transaction has on-chain conflict");
                            return;
                        }
                        unverified.Add(tx);
                    }
                }
            }
            foreach (Transaction tx in unverified)
                if (!AddTransaction(tx, true))
                    return;
            if (context.Transactions.Count < context.TransactionHashes.Length)
            {
                UInt256[] hashes = context.TransactionHashes.Where(i => !context.Transactions.ContainsKey(i)).ToArray();
                taskManager.Tell(new TaskManager.RestartTasks
                {
                    Payload = InvPayload.Create(InventoryType.TX, hashes)
                });
            }
        }

        private void OnPrepareResponseReceived(ExtensiblePayload payload, PrepareResponse message)
        {
            if (message.ViewNumber != context.ViewNumber) return;
            if (context.PreparationPayloads[message.ValidatorIndex] != null || context.NotAcceptingPayloadsDueToViewChanging) return;
            if (context.PreparationPayloads[context.Block.PrimaryIndex] != null && !message.PreparationHash.Equals(context.PreparationPayloads[context.Block.PrimaryIndex].Hash))
                return;

            // Timeout extension: prepare response has been received with success
            // around 2*15/M=30.0/5 ~ 40% block time (for M=5)
            ExtendTimerByFactor(2);

            _log.Information("PrepareResponse received: Height={Height}, View={View}, SenderIndex={Index}",
                message.BlockIndex, message.ViewNumber, message.ValidatorIndex);
            context.PreparationPayloads[message.ValidatorIndex] = payload;
            if (context.WatchOnly || context.CommitSent) return;
            if (context.RequestSentOrReceived)
                CheckPreparations();
        }

        private void OnChangeViewReceived(ExtensiblePayload payload, ChangeView message)
        {
            if (message.NewViewNumber <= context.ViewNumber)
                OnRecoveryRequestReceived(payload, message);

            if (context.CommitSent) return;

            var expectedView = context.GetMessage<ChangeView>(context.ChangeViewPayloads[message.ValidatorIndex])?.NewViewNumber ?? 0;
            if (message.NewViewNumber <= expectedView)
                return;

            _log.Warning("ChangeView received: Height={Height}, View={View}, SenderIndex={Index}, NewView={NewView}, Reason={Reason}",
                message.BlockIndex, message.ViewNumber, message.ValidatorIndex, message.NewViewNumber, message.Reason);
            context.ChangeViewPayloads[message.ValidatorIndex] = payload;
            CheckExpectedView(message.NewViewNumber);
        }

        private void OnCommitReceived(ExtensiblePayload payload, Commit commit)
        {
            ref ExtensiblePayload existingCommitPayload = ref context.CommitPayloads[commit.ValidatorIndex];
            if (existingCommitPayload != null)
            {
                if (existingCommitPayload.Hash != payload.Hash)
                    _log.Warning("Rejected {Commit}: height={Height} index={Index} view={View} existingView={ExistingView}", commit.BlockIndex, commit.ValidatorIndex, commit.ViewNumber, context.GetMessage(existingCommitPayload).ViewNumber);
                return;
            }

            if (commit.ViewNumber == context.ViewNumber)
            {
                // Timeout extension: commit has been received with success
                // around 4*15s/M=60.0s/5=12.0s ~ 80% block time (for M=5)
                ExtendTimerByFactor(4);

                _log.Information("Commit received: Height={Height}, View={View}, SenderIndex={Index}",
                    commit.BlockIndex, commit.ViewNumber, commit.ValidatorIndex);

                byte[] hashData = context.EnsureHeader()?.GetSignData(neoSystem.Settings.Network);
                if (hashData == null)
                {
                    existingCommitPayload = payload;
                }
                else if (Crypto.VerifySignature(hashData, commit.Signature.Span, context.Validators[commit.ValidatorIndex]))
                {
                    existingCommitPayload = payload;
                    CheckCommits();
                }
                return;
            }
            else
            {
                // Receiving commit from another view
                existingCommitPayload = payload;
            }
        }

        private void OnRecoveryMessageReceived(RecoveryMessage message)
        {
            // isRecovering is always set to false again after OnRecoveryMessageReceived
            isRecovering = true;
            int validChangeViews = 0, totalChangeViews = 0, validPrepReq = 0, totalPrepReq = 0;
            int validPrepResponses = 0, totalPrepResponses = 0, validCommits = 0, totalCommits = 0;

            _log.Information("Processing Recovery message for Height={Height}", context.Block.Index);
            try
            {
                if (message.ViewNumber > context.ViewNumber)
                {
                    if (context.CommitSent) return;
                    ExtensiblePayload[] changeViewPayloads = message.GetChangeViewPayloads(context);
                    totalChangeViews = changeViewPayloads.Length;
                    foreach (ExtensiblePayload changeViewPayload in changeViewPayloads)
                        if (ReverifyAndProcessPayload(changeViewPayload)) validChangeViews++;
                }
                if (message.ViewNumber == context.ViewNumber && !context.NotAcceptingPayloadsDueToViewChanging && !context.CommitSent)
                {
                    if (!context.RequestSentOrReceived)
                    {
                        ExtensiblePayload prepareRequestPayload = message.GetPrepareRequestPayload(context);
                        if (prepareRequestPayload != null)
                        {
                            totalPrepReq = 1;
                            if (ReverifyAndProcessPayload(prepareRequestPayload)) validPrepReq++;
                        }
                    }
                    ExtensiblePayload[] prepareResponsePayloads = message.GetPrepareResponsePayloads(context);
                    totalPrepResponses = prepareResponsePayloads.Length;
                    foreach (ExtensiblePayload prepareResponsePayload in prepareResponsePayloads)
                        if (ReverifyAndProcessPayload(prepareResponsePayload)) validPrepResponses++;
                }
                if (message.ViewNumber <= context.ViewNumber)
                {
                    // Ensure we know about all commits from lower view numbers.
                    ExtensiblePayload[] commitPayloads = message.GetCommitPayloadsFromRecoveryMessage(context);
                    totalCommits = commitPayloads.Length;
                    foreach (ExtensiblePayload commitPayload in commitPayloads)
                        if (ReverifyAndProcessPayload(commitPayload)) validCommits++;
                }
            }
            finally
            {
                _log.Information("Recovery finished: (valid/total) ChgView: {ValidChangeViews}/{TotalChangeViews} PrepReq: {ValidPrepReq}/{TotalPrepReq} PrepResp: {ValidPrepResponses}/{TotalPrepResponses} Commits: {ValidCommits}/{TotalCommits}",
                    validChangeViews, totalChangeViews, validPrepReq, totalPrepReq, validPrepResponses, totalPrepResponses, validCommits, totalCommits);
                isRecovering = false;
            }
        }

        private void OnRecoveryRequestReceived(ExtensiblePayload payload, ConsensusMessage message)
        {
            // We keep track of the payload hashes received in this block, and don't respond with recovery
            // in response to the same payload that we already responded to previously.
            // ChangeView messages include a Timestamp when the change view is sent, thus if a node restarts
            // and issues a change view for the same view, it will have a different hash and will correctly respond
            // again; however replay attacks of the ChangeView message from arbitrary nodes will not trigger an
            // additional recovery message response.
            if (!knownHashes.Add(payload.Hash)) return;

            if (message is RecoveryRequest recoveryRequest)
            {
                _log.Information("RecoveryRequest received: Height={Height}, View={View}, SenderIndex={Index}, Timestamp={Timestamp}",
                    message.BlockIndex, message.ViewNumber, message.ValidatorIndex, recoveryRequest.Timestamp);
            }
            else
            {
                _log.Information("Consensus message received triggering recovery logic: Type={MsgType}, Height={Height}, View={View}, SenderIndex={Index}",
                    message.GetType().Name, message.BlockIndex, message.ViewNumber, message.ValidatorIndex);
            }

            if (context.WatchOnly) return;
            if (!context.CommitSent)
            {
                bool shouldSendRecovery = false;
                int allowedRecoveryNodeCount = context.F + 1;
                // Limit recoveries to be sent from an upper limit of `f + 1` nodes
                for (int i = 1; i <= allowedRecoveryNodeCount; i++)
                {
                    var chosenIndex = (message.ValidatorIndex + i) % context.Validators.Length;
                    if (chosenIndex != context.MyIndex) continue;
                    shouldSendRecovery = true;
                    break;
                }

                if (!shouldSendRecovery) return;
            }
            localNode.Tell(new LocalNode.SendDirectly { Inventory = context.MakeRecoveryMessage() });
        }
    }
}
