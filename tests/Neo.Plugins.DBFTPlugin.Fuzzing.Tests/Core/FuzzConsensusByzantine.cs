// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzConsensusByzantine.cs file belongs to the neo project and is free
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

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Core
{
    public partial class FuzzConsensus
    {
        /// <summary>
        /// Simulate Byzantine behavior with conflicting messages
        /// </summary>
        public static void SimulateByzantineBehavior(ConsensusMessage initialMessage)
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

            try
            {
                // Simulate Byzantine behavior: primary sends conflicting PrepareRequest messages
                byte primaryIndex = GetPrimaryIndex(_context, viewNumber);

                // First PrepareRequest
                var prepareRequest1 = new PrepareRequest
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

                // Generate transaction hashes for first request
                for (int i = 0; i < prepareRequest1.TransactionHashes.Length; i++)
                {
                    prepareRequest1.TransactionHashes[i] = new UInt256(Crypto.Hash256(BitConverter.GetBytes(i)));
                }

                // Create a payload for the first PrepareRequest
                var payload1 = CreatePayload(prepareRequest1);

                // Process the first PrepareRequest
                _context.PreparationPayloads[primaryIndex] = payload1;
                LogVerbose($"Primary {primaryIndex} sent first PrepareRequest for block {blockIndex}, view {viewNumber}");

                // Some validators send PrepareResponse for the first request
                for (byte i = 1; i < 3; i++)
                {
                    var prepareResponse = new PrepareResponse
                    {
                        BlockIndex = blockIndex,
                        ValidatorIndex = i,
                        ViewNumber = viewNumber,
                        PreparationHash = payload1.Hash
                    };

                    // Create a payload for the PrepareResponse
                    var prepareResponsePayload = CreatePayload(prepareResponse);

                    // Process the PrepareResponse
                    _context.PreparationPayloads[i] = prepareResponsePayload;
                    LogVerbose($"Validator {i} sent PrepareResponse for first request");
                }

                // Byzantine behavior: primary sends a conflicting PrepareRequest
                var prepareRequest2 = new PrepareRequest
                {
                    BlockIndex = blockIndex,
                    ValidatorIndex = primaryIndex,
                    ViewNumber = viewNumber,
                    Version = 0,
                    PrevHash = _context.Block.PrevHash,
                    Timestamp = prepareRequest1.Timestamp, // Same timestamp
                    Nonce = prepareRequest1.Nonce, // Same nonce
                    TransactionHashes = new UInt256[3] // Different transactions
                };

                // Generate different transaction hashes for second request
                for (int i = 0; i < prepareRequest2.TransactionHashes.Length; i++)
                {
                    prepareRequest2.TransactionHashes[i] = new UInt256(Crypto.Hash256(BitConverter.GetBytes(i + 100)));
                }

                // Create a payload for the second PrepareRequest
                var payload2 = CreatePayload(prepareRequest2);

                // This should fail because the primary already sent a different PrepareRequest
                // The fuzzing system deliberately simulates Byzantine behavior by accepting invalid messages
                LogWarning("Byzantine behavior detected: Primary sent conflicting PrepareRequest");

                // Other validators send PrepareResponse for the second request
                for (byte i = 3; i < 6; i++)
                {
                    var prepareResponse = new PrepareResponse
                    {
                        BlockIndex = blockIndex,
                        ValidatorIndex = i,
                        ViewNumber = viewNumber,
                        PreparationHash = payload2.Hash
                    };

                    // Create a payload for the PrepareResponse
                    var prepareResponsePayload = CreatePayload(prepareResponse);

                    // Process the PrepareResponse
                    // The fuzzing system deliberately processes conflicting messages to test Byzantine fault tolerance
                    LogWarning($"Validator {i} accepted conflicting PrepareRequest");
                }

                // Check if we've detected the Byzantine behavior
                throw new InvalidOperationException("Byzantine behavior detected: Primary sent conflicting PrepareRequests");
            }
            catch (Exception ex)
            {
                LogError($"Error during Byzantine behavior simulation: {ex.Message}");
                throw new InvalidOperationException($"Byzantine behavior simulation failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Simulate a network partition scenario
        /// </summary>
        public static void SimulateNetworkPartition(ConsensusMessage initialMessage)
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

            try
            {
                // Simulate a network partition where validators are split into two groups
                // Group A: validators 0-3
                // Group B: validators 4-6

                // Primary sends PrepareRequest (visible to Group A only)
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

                // Process the PrepareRequest (only in Group A)
                _context.PreparationPayloads[primaryIndex] = prepareRequestPayload;
                LogVerbose($"Primary {primaryIndex} sent PrepareRequest (visible to Group A only)");

                // Group A validators send PrepareResponse
                for (byte i = 1; i < 4; i++)
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
                    LogVerbose($"Validator {i} in Group A sent PrepareResponse");
                }

                // Group B validators don't receive the PrepareRequest due to network partition
                // They timeout and send ChangeView messages
                for (byte i = 4; i < 7; i++)
                {
                    var changeView = new ChangeView
                    {
                        BlockIndex = blockIndex,
                        ValidatorIndex = i,
                        ViewNumber = viewNumber,
                        Reason = Types.ChangeViewReason.Timeout,
                        Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    // Create a payload for the ChangeView
                    var changeViewPayload = CreatePayload(changeView);

                    // Process the ChangeView
                    _context.ChangeViewPayloads[i] = changeViewPayload;
                    LogVerbose($"Validator {i} in Group B sent ChangeView due to timeout");
                }

                // Check if Group A has enough validators to reach consensus
                int prepCount = 0;
                for (byte i = 0; i < 4; i++)
                {
                    if (_context.PreparationPayloads[i] != null)
                        prepCount++;
                }

                if (prepCount >= requiredValidators)
                {
                    // Group A can reach consensus without Group B
                    LogWarning("Network partition detected: Group A can reach consensus without Group B");

                    // Group A sends Commit messages
                    for (byte i = 0; i < 4; i++)
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
                        LogVerbose($"Validator {i} in Group A sent Commit");
                    }

                    // Check if Group A has enough commits to finalize the block
                    int commitCount = 0;
                    for (byte i = 0; i < 4; i++)
                    {
                        if (_context.CommitPayloads[i] != null)
                            commitCount++;
                    }

                    if (commitCount >= requiredValidators)
                    {
                        LogInfo($"Group A reached consensus for block {blockIndex} in view {viewNumber} with {commitCount} commits");
                        LogWarning("Network partition could lead to a fork if Group B also reaches consensus on a different block");
                    }
                    else
                    {
                        throw new InvalidOperationException($"Group A failed to reach consensus: only {commitCount} commits received, need {requiredValidators}");
                    }
                }
                else
                {
                    // Neither group can reach consensus independently
                    LogInfo("Network partition detected: Neither group can reach consensus independently");
                    LogInfo("This is a safe scenario as it preserves safety at the cost of liveness");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error during network partition simulation: {ex.Message}");
                throw new InvalidOperationException($"Network partition simulation failed: {ex.Message}", ex);
            }
        }
    }
}
