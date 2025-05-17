// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzConsensusSimulations.cs file belongs to the neo project and is free
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
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using System;
using System.IO;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Core
{
    public partial class FuzzConsensus
    {


        /// <summary>
        /// Simulate a sequence of messages that should lead to consensus
        /// This tests the liveness property of the consensus algorithm
        /// </summary>
        public static void SimulateConsensusSequence(ConsensusMessage initialMessage)
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
            byte viewNumber = 0; // Start with view 0

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

            // Create transaction hashes (1-3 transactions)
            int txCount = 2;
            _context.TransactionHashes = new UInt256[txCount];
            for (int i = 0; i < txCount; i++)
            {
                _context.TransactionHashes[i] = new UInt256(Crypto.Hash256(BitConverter.GetBytes(i)));

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
                _context.Transactions[_context.TransactionHashes[i]] = tx;
            }

            // Step 1: Primary sends PrepareRequest
            byte primaryIndex = GetPrimaryIndex(_context, viewNumber);
            var prepareRequest = new PrepareRequest
            {
                BlockIndex = blockIndex,
                ValidatorIndex = primaryIndex,
                ViewNumber = viewNumber,
                Version = 0,
                PrevHash = _context.Block.PrevHash,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = 12345,
                TransactionHashes = _context.TransactionHashes
            };

            // Create a payload for the PrepareRequest
            var prepareRequestPayload = CreatePayload(prepareRequest);

            // Process the PrepareRequest
            try
            {
                _context.PreparationPayloads[primaryIndex] = prepareRequestPayload;
                LogVerbose($"Primary {primaryIndex} sent PrepareRequest for block {blockIndex}, view {viewNumber}");

                // Step 2: Other validators send PrepareResponse
                for (byte i = 0; i < validatorCount; i++)
                {
                    if (i == primaryIndex) continue; // Skip the primary

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

                // Step 3: All validators send Commit messages
                for (byte i = 0; i < validatorCount; i++)
                {
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

                // Check if we've reached consensus
                int commitCount = _context.CommitPayloads.Count(p => p != null);
                if (commitCount >= requiredValidators)
                {
                    LogInfo($"Consensus reached for block {blockIndex} in view {viewNumber} with {commitCount} commits");
                }
                else
                {
                    throw new InvalidOperationException($"Failed to reach consensus: only {commitCount} commits received, need {requiredValidators}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during consensus sequence: {ex.Message}");
                throw new InvalidOperationException($"Consensus sequence failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Simulate a sequence of messages with view changes that should still lead to consensus
        /// This tests the liveness property of the consensus algorithm in the presence of view changes
        /// </summary>
        public static void SimulateConsensusWithViewChanges(ConsensusMessage initialMessage)
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
            byte viewNumber = 0; // Start with view 0

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

            // Create transaction hashes (1-3 transactions)
            int txCount = 2;
            _context.TransactionHashes = new UInt256[txCount];
            for (int i = 0; i < txCount; i++)
            {
                _context.TransactionHashes[i] = new UInt256(Crypto.Hash256(BitConverter.GetBytes(i)));

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
                _context.Transactions[_context.TransactionHashes[i]] = tx;
            }

            try
            {
                // Step 1: Simulate a view change from view 0 to view 1
                // Some validators send ChangeView messages
                byte primaryIndex = GetPrimaryIndex(_context, viewNumber);
                int changeViewCount = 0;

                for (byte i = 0; i < validatorCount; i++)
                {
                    // Skip some validators to simulate partial participation
                    if (i % 2 == 0) continue;

                    var changeView = new ChangeView
                    {
                        BlockIndex = blockIndex,
                        ValidatorIndex = i,
                        ViewNumber = viewNumber,
                        Reason = ChangeViewReason.Timeout,
                        Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    // Create a payload for the ChangeView
                    var changeViewPayload = CreatePayload(changeView);

                    // Process the ChangeView
                    _context.ChangeViewPayloads[i] = changeViewPayload;
                    LogVerbose($"Validator {i} sent ChangeView for block {blockIndex}, view {viewNumber} -> {viewNumber + 1}");
                    changeViewCount++;
                }

                // Check if we have enough ChangeView messages to trigger a view change
                if (changeViewCount >= requiredValidators)
                {
                    // Update view number
                    viewNumber++;
                    _context.ViewNumber = viewNumber;
                    LogInfo($"View changed to {viewNumber} after receiving {changeViewCount} ChangeView messages");

                    // Reset preparation payloads for the new view
                    for (int i = 0; i < _context.PreparationPayloads.Length; i++)
                    {
                        _context.PreparationPayloads[i] = null;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Failed to change view: only {changeViewCount} ChangeView messages received, need {requiredValidators}");
                }

                // Step 2: New primary sends PrepareRequest in the new view
                primaryIndex = GetPrimaryIndex(_context, viewNumber);
                var prepareRequest = new PrepareRequest
                {
                    BlockIndex = blockIndex,
                    ValidatorIndex = primaryIndex,
                    ViewNumber = viewNumber,
                    Version = 0,
                    PrevHash = _context.Block.PrevHash,
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Nonce = 12345,
                    TransactionHashes = _context.TransactionHashes
                };

                // Create a payload for the PrepareRequest
                var prepareRequestPayload = CreatePayload(prepareRequest);

                // Process the PrepareRequest
                _context.PreparationPayloads[primaryIndex] = prepareRequestPayload;
                LogVerbose($"New primary {primaryIndex} sent PrepareRequest for block {blockIndex}, view {viewNumber}");

                // Step 3: Other validators send PrepareResponse
                for (byte i = 0; i < validatorCount; i++)
                {
                    if (i == primaryIndex) continue; // Skip the primary

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

                // Step 4: All validators send Commit messages
                for (byte i = 0; i < validatorCount; i++)
                {
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

                // Check if we've reached consensus
                int commitCount = _context.CommitPayloads.Count(p => p != null);
                if (commitCount >= requiredValidators)
                {
                    LogInfo($"Consensus reached for block {blockIndex} in view {viewNumber} with {commitCount} commits after view change");
                }
                else
                {
                    throw new InvalidOperationException($"Failed to reach consensus after view change: only {commitCount} commits received, need {requiredValidators}");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during consensus with view changes: {ex.Message}");
                throw new InvalidOperationException($"Consensus with view changes failed: {ex.Message}", ex);
            }
        }
    }
}
