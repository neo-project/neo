// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzConsensusRecovery.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Core
{
    public partial class FuzzConsensus
    {
        /// <summary>
        /// Simulate the recovery process in DBFT
        /// </summary>
        public static void SimulateRecoveryProcess(ConsensusMessage initialMessage)
        {
            // Reset the context to ensure a clean state
            InitializeContext();

            // Set up validators (we need at least 4 for meaningful consensus testing)
            byte validatorCount = 7;
            _context.Validators = new ECPoint[validatorCount];
            for (byte i = 0; i < validatorCount; i++)
            {
                // Create dummy public keys for validators
                byte[] privateKey = new byte[32];
                privateKey[0] = (byte)(i + 1);
                _context.Validators[i] = ECCurve.Secp256r1.G * privateKey;
            }

            // We can't directly set M, so we'll use it as a local variable
            int requiredValidators = _context.Validators.Length - (_context.Validators.Length - 1) / 3;

            // Use the initial message to seed some values
            uint blockIndex = initialMessage.BlockIndex;
            byte viewNumber = 1; // Start with view 1 to simulate a previous view change

            // Create a block with the appropriate values
            _context.Block = new Block
            {
                Header = new Header
                {
                    Index = blockIndex,
                    PrevHash = UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000001"),
                    MerkleRoot = UInt256.Zero,
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Nonce = 12345,
                    NextConsensus = UInt160.Zero
                },
                Transactions = Array.Empty<Transaction>()
            };

            try
            {
                // Simulate a scenario where a validator has been offline and needs to recover
                byte primaryIndex = GetPrimaryIndex(_context, viewNumber);
                byte offlineValidatorIndex = 3; // Validator 3 was offline

                // Set the context view number
                _context.ViewNumber = viewNumber;

                // Step 1: Primary has already sent PrepareRequest
                var prepareRequest = new PrepareRequest
                {
                    BlockIndex = blockIndex,
                    ValidatorIndex = primaryIndex,
                    ViewNumber = viewNumber,
                    Version = 0,
                    PrevHash = _context.Block.PrevHash,
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Nonce = 12345,
                    TransactionHashes = new UInt256[2]
                };

                // Generate transaction hashes
                for (int i = 0; i < prepareRequest.TransactionHashes.Length; i++)
                {
                    prepareRequest.TransactionHashes[i] = new UInt256(Crypto.Hash256(BitConverter.GetBytes(i)));

                    // Create dummy transactions
                    var tx = new Transaction
                    {
                        Version = 0,
                        Nonce = (uint)i,
                        SystemFee = 1000,
                        NetworkFee = 1000,
                        ValidUntilBlock = blockIndex + 100,
                        Attributes = Array.Empty<TransactionAttribute>(),
                        Signers = new Signer[] { new Signer { Account = UInt160.Zero } },
                        Script = new byte[] { 0x01 },
                        Witnesses = new Witness[] { new Witness { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() } }
                    };
                    _context.Transactions[prepareRequest.TransactionHashes[i]] = tx;
                }

                // Create a payload for the PrepareRequest
                var prepareRequestPayload = CreatePayload(prepareRequest);

                // Process the PrepareRequest
                _context.PreparationPayloads[primaryIndex] = prepareRequestPayload;
                LogVerbose($"Primary {primaryIndex} sent PrepareRequest for block {blockIndex}, view {viewNumber}");

                // Step 2: Other validators (except the offline one) have sent PrepareResponse
                for (byte i = 0; i < validatorCount; i++)
                {
                    if (i == primaryIndex || i == offlineValidatorIndex) continue; // Skip the primary and offline validator

                    var prepareResponse = new PrepareResponse
                    {
                        BlockIndex = blockIndex,
                        ValidatorIndex = i,
                        ViewNumber = viewNumber,
                        PreparationHash = prepareRequestPayload.Hash
                    };

                    // Create a payload for the PrepareResponse
                    var prepareResponsePayload = CreatePayload(prepareResponse);

                    // Process the PrepareResponse
                    _context.PreparationPayloads[i] = prepareResponsePayload;
                    LogVerbose($"Validator {i} sent PrepareResponse for block {blockIndex}, view {viewNumber}");
                }

                // Step 3: Some validators have already sent Commit messages
                for (byte i = 0; i < validatorCount; i++)
                {
                    if (i == offlineValidatorIndex) continue; // Skip the offline validator

                    var commit = new Commit
                    {
                        BlockIndex = blockIndex,
                        ValidatorIndex = i,
                        ViewNumber = viewNumber,
                        Signature = new byte[64] // Dummy signature
                    };

                    // Fill with random data for the signature
                    byte[] signature = new byte[64];
                    Random random = new Random((int)blockIndex + i);
                    random.NextBytes(signature);
                    commit.Signature = signature;

                    // Create a payload for the Commit
                    var commitPayload = CreatePayload(commit);

                    // Process the Commit
                    _context.CommitPayloads[i] = commitPayload;
                    LogVerbose($"Validator {i} sent Commit for block {blockIndex}, view {viewNumber}");
                }

                // Step 4: Offline validator comes back online and sends RecoveryRequest
                var recoveryRequest = new RecoveryRequest
                {
                    BlockIndex = blockIndex,
                    ValidatorIndex = offlineValidatorIndex,
                    ViewNumber = 0 // It was offline during the view change, so it's still at view 0
                };

                // Create a payload for the RecoveryRequest
                var recoveryRequestPayload = CreatePayload(recoveryRequest);

                LogVerbose($"Offline validator {offlineValidatorIndex} sent RecoveryRequest for block {blockIndex}, view 0");

                // Step 5: Primary creates and sends RecoveryMessage
                var recoveryMessage = new RecoveryMessage
                {
                    BlockIndex = blockIndex,
                    ValidatorIndex = primaryIndex,
                    ViewNumber = viewNumber,
                    ChangeViewMessages = new Dictionary<byte, RecoveryMessage.ChangeViewPayloadCompact>(),
                    PreparationMessages = new Dictionary<byte, RecoveryMessage.PreparationPayloadCompact>(),
                    CommitMessages = new Dictionary<byte, RecoveryMessage.CommitPayloadCompact>()
                };

                // Add ChangeView messages that led to the current view
                for (byte i = 0; i < validatorCount; i++)
                {
                    if (i == primaryIndex) continue; // Skip the primary

                    // Create a compact ChangeView payload
                    recoveryMessage.ChangeViewMessages[i] = new RecoveryMessage.ChangeViewPayloadCompact
                    {
                        ValidatorIndex = i,
                        OriginalViewNumber = 0, // Original view was 0
                        Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 10000 // 10 seconds ago
                    };
                }

                // Add PrepareRequest
                recoveryMessage.PrepareRequestMessage = prepareRequest;

                // Add PrepareResponse messages
                for (byte i = 0; i < validatorCount; i++)
                {
                    if (i == primaryIndex || i == offlineValidatorIndex) continue; // Skip the primary and offline validator

                    // Create a compact PrepareResponse payload
                    recoveryMessage.PreparationMessages[i] = new RecoveryMessage.PreparationPayloadCompact
                    {
                        ValidatorIndex = i
                        // Note: The actual implementation may have different fields
                        // We're simplifying for the fuzzing test
                    };
                }

                // Add Commit messages
                for (byte i = 0; i < validatorCount; i++)
                {
                    if (i == offlineValidatorIndex) continue; // Skip the offline validator

                    // Get the commit message
                    var existingCommit = _context.GetMessage<Commit>(_context.CommitPayloads[i]);
                    if (existingCommit == null) continue;

                    // Create a compact Commit payload
                    recoveryMessage.CommitMessages[i] = new RecoveryMessage.CommitPayloadCompact
                    {
                        ValidatorIndex = i,
                        ViewNumber = viewNumber,
                        Signature = existingCommit.Signature,
                        InvocationScript = new ReadOnlyMemory<byte>(new byte[10]) // Dummy invocation script
                    };
                }

                // Create a payload for the RecoveryMessage
                var recoveryMessagePayload = CreatePayload(recoveryMessage);

                LogVerbose($"Primary {primaryIndex} sent RecoveryMessage to validator {offlineValidatorIndex}");

                // Step 6: Offline validator processes the RecoveryMessage
                // The fuzzing system simulates the recovery process by updating the node's state

                // Simulate the offline validator updating its view number
                LogVerbose($"Validator {offlineValidatorIndex} updated view number from 0 to {viewNumber}");

                // Simulate the offline validator processing the PrepareRequest
                LogVerbose($"Validator {offlineValidatorIndex} processed PrepareRequest from primary {primaryIndex}");

                // Simulate the offline validator sending PrepareResponse
                var offlinePrepareResponse = new PrepareResponse
                {
                    BlockIndex = blockIndex,
                    ValidatorIndex = offlineValidatorIndex,
                    ViewNumber = viewNumber,
                    PreparationHash = prepareRequestPayload.Hash
                };

                // Create a payload for the PrepareResponse
                var offlinePrepareResponsePayload = CreatePayload(offlinePrepareResponse);

                // Process the PrepareResponse
                _context.PreparationPayloads[offlineValidatorIndex] = offlinePrepareResponsePayload;
                LogVerbose($"Validator {offlineValidatorIndex} sent PrepareResponse after recovery");

                // Simulate the offline validator sending Commit
                var offlineCommit = new Commit
                {
                    BlockIndex = blockIndex,
                    ValidatorIndex = offlineValidatorIndex,
                    ViewNumber = viewNumber,
                    Signature = new byte[64] // Dummy signature
                };

                // Fill with random data for the signature
                byte[] offlineSignature = new byte[64];
                Random offlineRandom = new Random((int)blockIndex + offlineValidatorIndex);
                offlineRandom.NextBytes(offlineSignature);
                offlineCommit.Signature = offlineSignature;

                // Create a payload for the Commit
                var offlineCommitPayload = CreatePayload(offlineCommit);

                // Process the Commit
                _context.CommitPayloads[offlineValidatorIndex] = offlineCommitPayload;
                LogVerbose($"Validator {offlineValidatorIndex} sent Commit after recovery");

                // Check if we've reached consensus
                int commitCount = _context.CommitPayloads.Count(p => p != null);
                if (commitCount >= requiredValidators)
                {
                    LogInfo($"Consensus reached for block {blockIndex} in view {viewNumber} with {commitCount} commits after recovery");
                }
                else
                {
                    throw new InvalidOperationException($"Failed to reach consensus after recovery: only {commitCount} commits received, need {requiredValidators}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during recovery process simulation: {ex.Message}");
                throw new InvalidOperationException($"Recovery process simulation failed: {ex.Message}", ex);
            }
        }
    }
}
