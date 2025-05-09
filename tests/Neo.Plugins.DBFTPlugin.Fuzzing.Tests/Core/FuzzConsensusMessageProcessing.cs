// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzConsensusMessageProcessing.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Cryptography;
using Neo.Extensions;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Core
{
    public partial class FuzzConsensus
    {
        private static void SimulateMessageProcessing(ExtensiblePayload payload, ConsensusMessage message)
        {
            // Basic checks applicable to most messages
            if (message.BlockIndex != _context.Block.Index)
            {
                // Allow fuzzing messages for future blocks within a reasonable range
                if (message.BlockIndex > _context.Block.Index && message.BlockIndex <= _context.Block.Index + 3)
                {
                    LogVerbose($"Message for future block: current={_context.Block.Index}, message={message.BlockIndex}");
                    // Continue processing to test handling of future block messages
                }
                else
                {
                    return; // Ignore messages for blocks too far ahead or behind
                }
            }

            if (message.ValidatorIndex >= _context.Validators.Length) return;

            // Attempt to simulate state transitions based on message type
            try
            {
                switch (message)
                {
                    case PrepareRequest request:
                        ProcessPrepareRequest(payload, request);
                        break;

                    case PrepareResponse response:
                        ProcessPrepareResponse(payload, response);
                        break;

                    case ChangeView view:
                        ProcessChangeView(payload, view);
                        break;

                    case Commit commit:
                        ProcessCommit(payload, commit);
                        break;

                    case RecoveryRequest recoveryRequest:
                        ProcessRecoveryRequest(payload, recoveryRequest);
                        break;

                    case RecoveryMessage recoveryMessage:
                        ProcessRecoveryMessage(payload, recoveryMessage);
                        break;
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                // Expected exceptions during fuzzing - rethrow to be caught by caller
                throw;
            }
            catch (Exception ex)
            {
                // Unexpected exceptions - log and rethrow as InvalidOperationException
                LogError($"Unexpected exception during message processing: {ex.GetType().Name}: {ex.Message}");
                throw new InvalidOperationException($"Unexpected exception: {ex.Message}", ex);
            }
        }

        private static void ProcessPrepareRequest(ExtensiblePayload payload, PrepareRequest request)
        {
            // Enhanced checks from OnPrepareRequestReceived
            if (_context.RequestSentOrReceived)
            {
                throw new InvalidOperationException("Received duplicate PrepareRequest");
            }

            if (_context.NotAcceptingPayloadsDueToViewChanging)
            {
                LogVerbose("Not accepting PrepareRequest due to view changing");
                return;
            }

            // Calculate primary index manually since Block.PrimaryIndex is read-only
            byte primaryIndex = GetPrimaryIndex(_context, _context.ViewNumber);
            if (request.ValidatorIndex != primaryIndex)
            {
                throw new InvalidOperationException($"PrepareRequest from non-primary validator: {request.ValidatorIndex}");
            }

            if (request.ViewNumber != _context.ViewNumber)
            {
                LogVerbose($"PrepareRequest for different view: current={_context.ViewNumber}, message={request.ViewNumber}");
                return;
            }

            if (request.TransactionHashes.Length > _system.Settings.MaxTransactionsPerBlock)
            {
                throw new ArgumentException($"Too many transactions in PrepareRequest: {request.TransactionHashes.Length}");
            }

            // Timestamp validation with more detailed error reporting
            var now = TimeProvider.Current.UtcNow.ToTimestampMS();
            if (request.Timestamp > now + 10000) // Allow some future time (10 seconds)
            {
                throw new ArgumentException($"PrepareRequest timestamp too far in future: {request.Timestamp}, now: {now}");
            }

            if (request.Timestamp < now - 30000) // Reject timestamps too far in the past (30 seconds)
            {
                throw new ArgumentException($"PrepareRequest timestamp too far in past: {request.Timestamp}, now: {now}");
            }

            // Simulate setting context fields
            // Create a new block with updated properties since Block properties are read-only
            var updatedBlock = new Block
            {
                Header = new Header
                {
                    Index = _context.Block.Index,
                    PrevHash = _context.Block.PrevHash,
                    MerkleRoot = _context.Block.MerkleRoot,
                    Timestamp = request.Timestamp,
                    Nonce = request.Nonce,
                    NextConsensus = _context.Block.NextConsensus
                },
                Transactions = _context.Block.Transactions
            };
            _context.Block = updatedBlock;
            _context.TransactionHashes = request.TransactionHashes;
            _context.PreparationPayloads[request.ValidatorIndex] = payload;

            // Simulate adding transactions (simplified)
            foreach (var hash in request.TransactionHashes)
            {
                if (!_context.Transactions.ContainsKey(hash))
                {
                    // Create a dummy transaction for testing with proper initialization
                    var tx = new Transaction
                    {
                        Version = 0,
                        Nonce = 0,
                        SystemFee = 0,
                        NetworkFee = 0,
                        ValidUntilBlock = 0,
                        Attributes = Array.Empty<TransactionAttribute>(),
                        Signers = Array.Empty<Signer>(),
                        Script = Array.Empty<byte>(),
                        Witnesses = new Witness[1] { new Witness { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() } }
                    };
                    // Transaction hash is computed automatically
                    _context.Transactions[hash] = tx;
                }
            }

            LogVerbose($"Processed PrepareRequest with {request.TransactionHashes.Length} transactions");
        }

        private static void ProcessPrepareResponse(ExtensiblePayload payload, PrepareResponse response)
        {
            if (response.ViewNumber != _context.ViewNumber)
            {
                LogVerbose($"PrepareResponse for different view: current={_context.ViewNumber}, message={response.ViewNumber}");
                return;
            }

            if (_context.PreparationPayloads[response.ValidatorIndex] != null)
            {
                throw new InvalidOperationException($"Duplicate PrepareResponse from validator {response.ValidatorIndex}");
            }

            if (_context.NotAcceptingPayloadsDueToViewChanging)
            {
                LogVerbose("Not accepting PrepareResponse due to view changing");
                return;
            }

            // Simplified hash check - requires PrepareRequest to be processed first
            var primaryPayload = _context.PreparationPayloads[_context.Block.PrimaryIndex];
            if (primaryPayload == null)
            {
                throw new InvalidOperationException("Received PrepareResponse before PrepareRequest");
            }

            if (!response.PreparationHash.Equals(primaryPayload.Hash))
            {
                throw new InvalidOperationException("PrepareResponse hash doesn't match PrepareRequest hash");
            }

            _context.PreparationPayloads[response.ValidatorIndex] = payload;

            // Simulate CheckPreparations logic
            int prepCount = _context.PreparationPayloads.Count(p => p != null);
            LogVerbose($"Processed PrepareResponse, preparation count: {prepCount}/{_context.M}");

            // If we have enough preparations, simulate block commit
            if (prepCount >= _context.M && !_context.CommitSent && _context.TransactionHashes.All(h => _context.Transactions.ContainsKey(h)))
            {
                LogVerbose("Preparation threshold reached, ready for commit");
            }
        }

        private static void ProcessChangeView(ExtensiblePayload payload, ChangeView view)
        {
            if (view.NewViewNumber <= _context.ViewNumber)
            {
                LogVerbose($"Ignoring ChangeView with old/current view: {view.NewViewNumber}");
                return;
            }

            if (_context.CommitSent)
            {
                LogVerbose("Ignoring ChangeView after commit sent");
                return;
            }

            var expectedView = _context.GetMessage<ChangeView>(_context.ChangeViewPayloads[view.ValidatorIndex])?.NewViewNumber ?? 0;
            if (view.NewViewNumber <= expectedView)
            {
                LogVerbose($"Ignoring ChangeView with view number <= expected: {view.NewViewNumber} <= {expectedView}");
                return;
            }

            _context.ChangeViewPayloads[view.ValidatorIndex] = payload;

            // Simulate CheckExpectedView logic
            int viewChanges = _context.ChangeViewPayloads.Count(p => p != null &&
                _context.GetMessage<ChangeView>(p)?.NewViewNumber > _context.ViewNumber);

            LogVerbose($"Processed ChangeView to {view.NewViewNumber}, change count: {viewChanges}/{_context.M}");

            // If we have enough view changes, simulate view change
            if (viewChanges >= _context.M)
            {
                // Find the highest view number that has M consensus
                byte highestView = view.NewViewNumber;
                _context.ViewNumber = highestView;
                LogVerbose($"View change threshold reached, changing to view {highestView}");

                // Reset context for new view
                for (int i = 0; i < _context.PreparationPayloads.Length; i++)
                    _context.PreparationPayloads[i] = null;

                // Cannot directly set PrimaryIndex as it's read-only
                // Calculate the primary index for the new view
                byte newPrimaryIndex = GetPrimaryIndex(_context, _context.ViewNumber);
                LogVerbose($"New primary for view {_context.ViewNumber}: {newPrimaryIndex}");
                // Recreate the block with the new primary index
                var updatedBlock = new Block
                {
                    Header = new Header
                    {
                        Index = _context.Block.Index,
                        PrevHash = _context.Block.PrevHash,
                        MerkleRoot = _context.Block.MerkleRoot,
                        Timestamp = _context.Block.Timestamp,
                        Nonce = _context.Block.Nonce,
                        NextConsensus = _context.Block.NextConsensus
                    },
                    Transactions = _context.Block.Transactions
                };
                _context.Block = updatedBlock;

                // Reset transaction hashes for the new view
                _context.TransactionHashes = Array.Empty<UInt256>();

                // Log the view change for debugging
                LogInfo($"View changed to {highestView}, new primary is {newPrimaryIndex}");
            }
        }

        private static void ProcessCommit(ExtensiblePayload payload, Commit commit)
        {
            if (commit.ViewNumber != _context.ViewNumber)
            {
                LogVerbose($"Commit for different view: current={_context.ViewNumber}, message={commit.ViewNumber}");
                return;
            }

            if (_context.CommitSent)
            {
                LogVerbose("Already sent commit, ignoring");
                return;
            }

            if (_context.CommitPayloads[commit.ValidatorIndex] != null)
            {
                throw new InvalidOperationException($"Duplicate Commit from validator {commit.ValidatorIndex}");
            }

            // Simplified signature check (requires header data)
            try
            {
                byte[] hashData = _context.EnsureHeader().GetSignData(_system.Settings.Network);

                // Ensure commit.Signature is not empty before accessing Span
                if (commit.Signature.IsEmpty)
                {
                    throw new ArgumentException("Commit has empty signature");
                }

                if (!Crypto.VerifySignature(hashData, commit.Signature.ToArray(), _context.Validators[commit.ValidatorIndex]))
                {
                    throw new InvalidOperationException("Invalid signature in Commit message");
                }
            }
            catch (NullReferenceException)
            {
                throw new InvalidOperationException("Failed to verify commit signature - header not properly initialized");
            }

            _context.CommitPayloads[commit.ValidatorIndex] = payload;

            // Simulate CheckCommits logic
            int commitCount = _context.CommitPayloads.Count(p => p != null);
            LogVerbose($"Processed Commit, commit count: {commitCount}/{_context.M}");

            // If we have enough commits, simulate block finalization
            if (commitCount >= _context.M)
            {
                // Cannot directly set BlockSent as it's read-only
                // The fuzzing system simulates block finalization by logging the event
                LogVerbose("Commit threshold reached, block finalized");
            }
        }

        private static void ProcessRecoveryRequest(ExtensiblePayload payload, RecoveryRequest request)
        {
            // Basic validation for RecoveryRequest
            if (request.ViewNumber > _context.ViewNumber)
            {
                LogVerbose($"RecoveryRequest for future view: current={_context.ViewNumber}, request={request.ViewNumber}");
            }

            // Prepare and send a RecoveryMessage in response to the RecoveryRequest
            LogVerbose($"Received RecoveryRequest from validator {request.ValidatorIndex} for view {request.ViewNumber}");

            // Create a new RecoveryMessage
            var recoveryMessage = new RecoveryMessage
            {
                BlockIndex = _context.Block.Index,
                ValidatorIndex = (byte)_context.MyIndex,
                ViewNumber = _context.ViewNumber,
                ChangeViewMessages = new Dictionary<byte, RecoveryMessage.ChangeViewPayloadCompact>(),
                PreparationMessages = new Dictionary<byte, RecoveryMessage.PreparationPayloadCompact>(),
                CommitMessages = new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>()
            };

            // Add ChangeView messages that led to the current view
            for (byte i = 0; i < _context.ChangeViewPayloads.Length; i++)
            {
                if (_context.ChangeViewPayloads[i] != null)
                {
                    var changeView = _context.GetMessage<ChangeView>(_context.ChangeViewPayloads[i]);
                    if (changeView != null && changeView.NewViewNumber <= _context.ViewNumber)
                    {
                        // Create a compact ChangeView payload
                        recoveryMessage.ChangeViewMessages[i] = new RecoveryMessage.ChangeViewPayloadCompact
                        {
                            ValidatorIndex = i,
                            OriginalViewNumber = changeView.ViewNumber,
                            Timestamp = changeView.Timestamp
                        };
                    }
                }
            }

            // Add PrepareRequest if we have one
            var primaryIndex = GetPrimaryIndex(_context, _context.ViewNumber);
            if (_context.PreparationPayloads[primaryIndex] != null)
            {
                var prepareRequest = _context.GetMessage<PrepareRequest>(_context.PreparationPayloads[primaryIndex]);
                if (prepareRequest != null)
                {
                    recoveryMessage.PrepareRequestMessage = prepareRequest;
                }
            }

            // Add PrepareResponse messages
            for (byte i = 0; i < _context.PreparationPayloads.Length; i++)
            {
                if (i != primaryIndex && _context.PreparationPayloads[i] != null)
                {
                    var prepareResponse = _context.GetMessage<PrepareResponse>(_context.PreparationPayloads[i]);
                    if (prepareResponse != null && prepareResponse.ViewNumber == _context.ViewNumber)
                    {
                        // Create a compact PrepareResponse payload
                        recoveryMessage.PreparationMessages[i] = new RecoveryMessage.PreparationPayloadCompact
                        {
                            ValidatorIndex = i
                        };
                    }
                }
            }

            // Add Commit messages
            for (byte i = 0; i < _context.CommitPayloads.Length; i++)
            {
                if (_context.CommitPayloads[i] != null)
                {
                    var commit = _context.GetMessage<Commit>(_context.CommitPayloads[i]);
                    if (commit != null && commit.ViewNumber == _context.ViewNumber)
                    {
                        // Create a compact Commit payload
                        recoveryMessage.CommitMessages[i] = new RecoveryMessage.CommitPayloadCompact
                        {
                            ValidatorIndex = i,
                            ViewNumber = commit.ViewNumber,
                            Signature = commit.Signature,
                            InvocationScript = new ReadOnlyMemory<byte>(new byte[10]) // Dummy invocation script
                        };
                    }
                }
            }

            // Create a payload for the RecoveryMessage
            var recoveryMessagePayload = CreatePayload(recoveryMessage);

            // For fuzzing purposes, we log the creation of the RecoveryMessage rather than sending it over the network
            // This is sufficient for testing the message creation logic without network dependencies
            LogVerbose($"Created RecoveryMessage in response to request from validator {request.ValidatorIndex}");
        }

        private static void ProcessRecoveryMessage(ExtensiblePayload payload, RecoveryMessage recoveryMessage)
        {
            // Basic validation
            if (recoveryMessage.ViewNumber < _context.ViewNumber)
            {
                LogVerbose($"Ignoring RecoveryMessage for old view: {recoveryMessage.ViewNumber}");
                return;
            }

            // For fuzzing purposes, we'll simulate some basic recovery logic
            LogVerbose($"Processing RecoveryMessage from validator {recoveryMessage.ValidatorIndex}");

            // Process ChangeView messages from recovery
            if (recoveryMessage.ChangeViewMessages != null && recoveryMessage.ChangeViewMessages.Count > 0)
            {
                LogVerbose($"Recovery contains {recoveryMessage.ChangeViewMessages.Count} ChangeView messages");

                // Apply ChangeView messages to our context
                foreach (var kvp in recoveryMessage.ChangeViewMessages)
                {
                    byte validatorIndex = kvp.Key;
                    var changeViewCompact = kvp.Value;

                    // Validate the compact message
                    if (validatorIndex >= _context.Validators.Length)
                        continue;

                    // Only process if it's for a higher view than what we have
                    if (changeViewCompact.OriginalViewNumber >= _context.ViewNumber)
                    {
                        LogVerbose($"Applying ChangeView from validator {validatorIndex} to view {changeViewCompact.OriginalViewNumber + 1}");

                        // Create a proper ChangeView message
                        var changeView = new ChangeView
                        {
                            BlockIndex = _context.Block.Index,
                            ValidatorIndex = validatorIndex,
                            ViewNumber = changeViewCompact.OriginalViewNumber,
                            Reason = ChangeViewReason.Timeout,
                            Timestamp = changeViewCompact.Timestamp
                        };

                        // Create a payload for the ChangeView
                        var changeViewPayload = CreatePayload(changeView);

                        // Apply it to our context
                        _context.ChangeViewPayloads[validatorIndex] = changeViewPayload;

                        // Check if we need to update our view number
                        int viewChanges = _context.ChangeViewPayloads.Count(p => p != null &&
                            _context.GetMessage<ChangeView>(p)?.NewViewNumber > _context.ViewNumber);

                        if (viewChanges >= _context.M && changeViewCompact.OriginalViewNumber + 1 > _context.ViewNumber)
                        {
                            // Update our view number
                            _context.ViewNumber = (byte)(changeViewCompact.OriginalViewNumber + 1);
                            LogInfo($"Updated view number to {_context.ViewNumber} based on recovery message");

                            // Reset preparation payloads for the new view
                            for (int i = 0; i < _context.PreparationPayloads.Length; i++)
                            {
                                _context.PreparationPayloads[i] = null;
                            }
                        }
                    }
                }
            }

            // Process PrepareRequest from recovery
            if (recoveryMessage.PrepareRequestMessage != null)
            {
                LogVerbose("Recovery contains PrepareRequest");

                // Validate the PrepareRequest
                if (recoveryMessage.PrepareRequestMessage.ViewNumber == recoveryMessage.ViewNumber &&
                    recoveryMessage.PrepareRequestMessage.BlockIndex == _context.Block.Index)
                {
                    // Process the PrepareRequest from the recovery message
                    // Create a payload for the PrepareRequest
                    var prepareRequestPayload = CreatePayload(recoveryMessage.PrepareRequestMessage);

                    // Process the PrepareRequest
                    byte primaryIndex = GetPrimaryIndex(_context, recoveryMessage.ViewNumber);
                    if (recoveryMessage.PrepareRequestMessage.ValidatorIndex == primaryIndex)
                    {
                        // Only accept PrepareRequest from the primary for this view
                        _context.PreparationPayloads[primaryIndex] = prepareRequestPayload;

                        // Update our transaction hashes
                        _context.TransactionHashes = recoveryMessage.PrepareRequestMessage.TransactionHashes;

                        // Update block properties
                        var updatedBlock = new Block
                        {
                            Header = new Header
                            {
                                Index = _context.Block.Index,
                                PrevHash = recoveryMessage.PrepareRequestMessage.PrevHash,
                                MerkleRoot = _context.Block.MerkleRoot,
                                Timestamp = recoveryMessage.PrepareRequestMessage.Timestamp,
                                Nonce = recoveryMessage.PrepareRequestMessage.Nonce,
                                NextConsensus = _context.Block.NextConsensus
                            },
                            Transactions = _context.Block.Transactions
                        };
                        _context.Block = updatedBlock;

                        LogVerbose($"Processed PrepareRequest from primary {primaryIndex} via recovery message");
                    }
                    else
                    {
                        LogWarning($"Ignoring PrepareRequest from non-primary validator {recoveryMessage.PrepareRequestMessage.ValidatorIndex}");
                    }
                    if (recoveryMessage.PrepareRequestMessage.ValidatorIndex >= _context.Validators.Length)
                    {
                        throw new InvalidOperationException("RecoveryMessage PrepareRequest has invalid validator index");
                    }

                    if (recoveryMessage.PrepareRequestMessage.TransactionHashes == null)
                    {
                        throw new InvalidOperationException("RecoveryMessage PrepareRequest has null transaction hashes");
                    }

                    if (recoveryMessage.PrepareRequestMessage.TransactionHashes.Length > _system.Settings.MaxTransactionsPerBlock)
                    {
                        throw new InvalidOperationException($"RecoveryMessage PrepareRequest has too many transactions: {recoveryMessage.PrepareRequestMessage.TransactionHashes.Length}");
                    }
                }
                else
                {
                    throw new InvalidOperationException("RecoveryMessage PrepareRequest has inconsistent view number or block index");
                }
            }

            // Process PreparationHash from recovery
            if (recoveryMessage.PreparationHash != null && recoveryMessage.PreparationHash != UInt256.Zero)
            {
                LogVerbose("Recovery contains PreparationHash");

                // Use the hash to validate preparations
                // First, check if we have a primary's PrepareRequest
                byte primaryIndex = GetPrimaryIndex(_context, recoveryMessage.ViewNumber);
                if (_context.PreparationPayloads[primaryIndex] != null)
                {
                    // Verify that the hash matches
                    if (!_context.PreparationPayloads[primaryIndex].Hash.Equals(recoveryMessage.PreparationHash))
                    {
                        LogWarning("PreparationHash in recovery message doesn't match our primary's PrepareRequest");
                        // We detect and log the hash mismatch for testing purposes
                        // This allows us to verify the hash validation logic without network request simulation
                    }
                    else
                    {
                        LogVerbose("PreparationHash in recovery message matches our primary's PrepareRequest");
                    }
                }
                else
                {
                    // We don't have the primary's PrepareRequest yet
                    // The fuzzing system simulates storing the hash for future validation
                    // This tests the recovery protocol's ability to handle out-of-order message delivery
                    LogVerbose("Storing PreparationHash for future validation of primary's PrepareRequest");
                }
            }

            // Process PreparationMessages from recovery
            if (recoveryMessage.PreparationMessages != null && recoveryMessage.PreparationMessages.Count > 0)
            {
                LogVerbose($"Recovery contains {recoveryMessage.PreparationMessages.Count} PreparationMessages");

                // Apply PreparationMessages to our context
                foreach (var kvp in recoveryMessage.PreparationMessages)
                {
                    byte validatorIndex = kvp.Key;
                    var prepCompact = kvp.Value;

                    // Validate the compact message
                    if (validatorIndex >= _context.Validators.Length)
                        continue;

                    LogVerbose($"Processing preparation from validator {validatorIndex}");

                    // Create a proper PrepareResponse message
                    // First, we need the hash of the primary's PrepareRequest
                    byte primaryIndex = GetPrimaryIndex(_context, recoveryMessage.ViewNumber);
                    if (_context.PreparationPayloads[primaryIndex] == null)
                    {
                        LogVerbose($"Cannot process PrepareResponse from validator {validatorIndex} - missing primary's PrepareRequest");
                        continue;
                    }

                    var prepareResponse = new PrepareResponse
                    {
                        BlockIndex = _context.Block.Index,
                        ValidatorIndex = validatorIndex,
                        ViewNumber = recoveryMessage.ViewNumber,
                        PreparationHash = _context.PreparationPayloads[primaryIndex].Hash
                    };

                    // Create a payload for the PrepareResponse
                    var prepareResponsePayload = CreatePayload(prepareResponse);

                    // Apply it to our context
                    _context.PreparationPayloads[validatorIndex] = prepareResponsePayload;

                    // Check if we've reached the preparation threshold
                    int prepCount = _context.PreparationPayloads.Count(p => p != null);
                    LogVerbose($"Processed PrepareResponse from validator {validatorIndex}, preparation count: {prepCount}/{_context.M}");

                    // If we have enough preparations and haven't sent a commit yet, we should send a commit
                    if (prepCount >= _context.M && !_context.CommitSent && _context.TransactionHashes.All(h => _context.Transactions.ContainsKey(h)))
                    {
                        LogVerbose("Preparation threshold reached via recovery message, ready for commit");
                    }
                }
            }

            // Process CommitMessages from recovery
            if (recoveryMessage.CommitMessages != null && recoveryMessage.CommitMessages.Count > 0)
            {
                LogVerbose($"Recovery contains {recoveryMessage.CommitMessages.Count} CommitMessages");

                // Apply CommitMessages to our context
                foreach (var kvp in recoveryMessage.CommitMessages)
                {
                    byte validatorIndex = kvp.Key;
                    var commitCompact = kvp.Value;

                    // Validate the compact message
                    if (validatorIndex >= _context.Validators.Length)
                        continue;

                    if (commitCompact.ViewNumber != recoveryMessage.ViewNumber)
                    {
                        LogVerbose($"Commit from validator {validatorIndex} has inconsistent view number: {commitCompact.ViewNumber}");
                        continue;
                    }

                    if (commitCompact.Signature.IsEmpty || commitCompact.Signature.Length != 64)
                    {
                        LogVerbose($"Commit from validator {validatorIndex} has invalid signature");
                        continue;
                    }

                    LogVerbose($"Processing commit from validator {validatorIndex}");

                    // Create a proper Commit message
                    var commit = new Commit
                    {
                        BlockIndex = _context.Block.Index,
                        ValidatorIndex = validatorIndex,
                        ViewNumber = commitCompact.ViewNumber,
                        Signature = commitCompact.Signature
                    };

                    // Create a payload for the Commit
                    var commitPayload = CreatePayload(commit);

                    // Apply it to our context
                    _context.CommitPayloads[validatorIndex] = commitPayload;

                    // Check if we've reached the commit threshold
                    int commitCount = _context.CommitPayloads.Count(p => p != null);
                    LogVerbose($"Processed Commit from validator {validatorIndex}, commit count: {commitCount}/{_context.M}");

                    // If we have enough commits, we should finalize the block
                    if (commitCount >= _context.M)
                    {
                        LogInfo($"Commit threshold reached via recovery message for block {_context.Block.Index} in view {_context.ViewNumber}");

                        // The fuzzing system simulates block finalization by logging the event
                        // This verifies that the consensus threshold detection works correctly
                        // without requiring actual block broadcasting
                        LogVerbose("Block finalized based on recovery message consensus");
                    }
                }
            }
        }

        /// <summary>
        /// Create an ExtensiblePayload for a consensus message
        /// </summary>
        private static ExtensiblePayload CreatePayload(ConsensusMessage message)
        {
            string category;
            switch (message)
            {
                case PrepareRequest _: category = "dbft.pr"; break;
                case PrepareResponse _: category = "dbft.ps"; break;
                case ChangeView _: category = "dbft.cv"; break;
                case Commit _: category = "dbft.commit"; break;
                case RecoveryRequest _: category = "dbft.rr"; break;
                case RecoveryMessage _: category = "dbft.rm"; break;
                default: throw new ArgumentException($"Unknown message type: {message.GetType().Name}");
            }

            var payload = new ExtensiblePayload
            {
                Category = category,
                Sender = UInt160.Zero,
                Witness = new Witness { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
            };

            // Serialize the consensus message
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                message.Serialize(writer);
                payload.Data = ms.ToArray();
            }

            return payload;
        }


    }
}
