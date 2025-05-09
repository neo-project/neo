// Copyright (C) 2015-2025 The Neo Project.
//
// CorpusGenerator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Utils;
using System;
using System.IO;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Generators
{
    /// <summary>
    /// Main corpus generator for DBFT fuzzing
    /// </summary>
    public static class CorpusGenerator
    {
        /// <summary>
        /// Generate corpus files for fuzzing
        /// </summary>
        /// <param name="outputDirectory">Directory to write corpus files</param>
        /// <param name="count">Number of files to generate per message type</param>
        public static void Generate(string outputDirectory, int count = 10)
        {
            Directory.CreateDirectory(outputDirectory);

            Console.WriteLine($"Generating {count} corpus files per message type...");

            // Generate corpus files for each message type
            for (int i = 0; i < count; i++)
            {
                // Basic message types
                CommitGenerator.Generate(outputDirectory, i);
                PrepareRequestGenerator.Generate(outputDirectory, i);
                PrepareResponseGenerator.Generate(outputDirectory, i);
                ChangeViewGenerator.Generate(outputDirectory, i);
                RecoveryRequestGenerator.Generate(outputDirectory, i);
                RecoveryMessageGenerator.Generate(outputDirectory, i);

                // Also generate some completely invalid messages
                GenerateInvalidMessage(outputDirectory, i);

                // Generate messages with different block indices
                if (i % 3 == 0)
                {
                    uint blockIndex = (uint)(1000 + i);
                    CommitGenerator.GenerateWithCustomBlockIndex(outputDirectory, blockIndex, $"block_{blockIndex}_commit_{i}.bin");
                    PrepareRequestGenerator.GenerateWithCustomBlockIndex(outputDirectory, blockIndex, $"block_{blockIndex}_prepare_request_{i}.bin");
                    PrepareResponseGenerator.GenerateWithCustomBlockIndex(outputDirectory, blockIndex, $"block_{blockIndex}_prepare_response_{i}.bin");
                }

                // Generate messages with different view numbers
                if (i % 4 == 0)
                {
                    byte viewNumber = (byte)(i % 10);
                    CommitGenerator.GenerateWithCustomViewNumber(outputDirectory, viewNumber, $"view_{viewNumber}_commit_{i}.bin");
                    PrepareRequestGenerator.GenerateWithCustomViewNumber(outputDirectory, viewNumber, $"view_{viewNumber}_prepare_request_{i}.bin");
                    PrepareResponseGenerator.GenerateWithCustomViewNumber(outputDirectory, viewNumber, $"view_{viewNumber}_prepare_response_{i}.bin");
                    ChangeViewGenerator.GenerateWithCustomViewNumber(outputDirectory, viewNumber, $"view_{viewNumber}_change_view_{i}.bin");
                }

                // Generate messages with different validator indices
                if (i % 5 == 0)
                {
                    byte validatorIndex = (byte)(i % 7);
                    CommitGenerator.GenerateWithCustomValidatorIndex(outputDirectory, validatorIndex, $"validator_{validatorIndex}_commit_{i}.bin");
                    PrepareRequestGenerator.GenerateWithCustomValidatorIndex(outputDirectory, validatorIndex, $"validator_{validatorIndex}_prepare_request_{i}.bin");
                    PrepareResponseGenerator.GenerateWithCustomValidatorIndex(outputDirectory, validatorIndex, $"validator_{validatorIndex}_prepare_response_{i}.bin");
                    ChangeViewGenerator.GenerateWithCustomValidatorIndex(outputDirectory, validatorIndex, $"validator_{validatorIndex}_change_view_{i}.bin");
                }

                // Progress indicator for large corpus generation
                if (i > 0 && i % 10 == 0)
                {
                    Console.WriteLine($"Generated {i * 7} corpus files so far...");
                }
            }

            Console.WriteLine("Generating edge cases and special scenarios...");
            // Generate edge cases and special scenarios
            GenerateEdgeCases(outputDirectory);

            // Calculate total files generated
            int basicFiles = count * 7; // 6 message types + invalid messages
            int blockFiles = (count / 3) * 3; // Every 3rd iteration generates 3 files
            int viewFiles = (count / 4) * 4; // Every 4th iteration generates 4 files
            int validatorFiles = (count / 5) * 4; // Every 5th iteration generates 4 files
            int edgeCaseFiles = 45; // Approximate number of edge case files
            int totalFiles = basicFiles + blockFiles + viewFiles + validatorFiles + edgeCaseFiles;

            Console.WriteLine($"Generated approximately {totalFiles} corpus files in {outputDirectory}");
        }

        private static void GenerateInvalidMessage(string outputDirectory, int index)
        {
            // Create completely random data that doesn't conform to any message format
            byte[] randomData = FuzzingHelpers.GenerateRandomBytes(FuzzingHelpers.Random.Next(10, 200));
            File.WriteAllBytes(Path.Combine(outputDirectory, $"invalid_{index}.bin"), randomData);
        }

        private static void GenerateEdgeCases(string outputDirectory)
        {
            // Generate specific edge cases that target known vulnerabilities and edge conditions

            // Basic edge cases
            // 1. Empty payload
            File.WriteAllBytes(Path.Combine(outputDirectory, "edge_empty.bin"), new byte[0]);

            // 2. Very large payload (just under the limit)
            byte[] largeData = FuzzingHelpers.GenerateRandomBytes(1024 * 9);
            File.WriteAllBytes(Path.Combine(outputDirectory, "edge_large.bin"), largeData);

            // 3. Payload with invalid category but valid message format
            PrepareRequestGenerator.GenerateWithCustomCategory(outputDirectory, "invalid_category");

            // 4. Payload with valid category but invalid message format
            byte[] invalidFormat = FuzzingHelpers.GenerateRandomBytes(100);
            MessageSerializer.WriteRawBytesToFile(outputDirectory, "edge_invalid_format.bin", invalidFormat, "dbft.pr");

            // Validator index edge cases
            // 5. Message with maximum validator index
            CommitGenerator.GenerateWithCustomValidatorIndex(outputDirectory, byte.MaxValue);

            // 6. Message with validator index just below the expected maximum
            CommitGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 6); // Assuming 7 validators

            // 7. Message with validator index one above the expected maximum
            CommitGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 7); // Assuming 7 validators

            // View number edge cases
            // 8. Message with maximum view number
            ChangeViewGenerator.GenerateWithCustomViewNumber(outputDirectory, byte.MaxValue);

            // 9. Message with view number 254
            ChangeViewGenerator.GenerateWithCustomViewNumber(outputDirectory, 254);

            // 10. Message with view number 255 (max byte value)
            ChangeViewGenerator.GenerateWithCustomViewNumber(outputDirectory, 255);

            // Transaction edge cases
            // 11. PrepareRequest with maximum transactions
            PrepareRequestGenerator.GenerateWithMaxTransactions(outputDirectory);

            // 12. PrepareRequest with almost maximum transactions
            PrepareRequestGenerator.GenerateWithCustomTransactionCount(outputDirectory, 500);

            // 13. PrepareRequest with zero transactions
            PrepareRequestGenerator.GenerateWithZeroTransactions(outputDirectory);

            // 14. PrepareRequest with one transaction
            PrepareRequestGenerator.GenerateWithCustomTransactionCount(outputDirectory, 1);

            // 15. PrepareRequest with duplicate transaction hashes
            PrepareRequestGenerator.GenerateWithDuplicateTransactions(outputDirectory);

            // Signature edge cases
            // 16. Commit with invalid signature
            CommitGenerator.GenerateWithInvalidSignature(outputDirectory);

            // 17. Commit with all-zero signature
            CommitGenerator.GenerateWithAllZeroSignature(outputDirectory);

            // 18. Commit with all-one signature
            CommitGenerator.GenerateWithAllOneSignature(outputDirectory);

            // Recovery message edge cases
            // 19. RecoveryMessage with inconsistent state
            RecoveryMessageGenerator.GenerateWithInconsistentState(outputDirectory);

            // 20. RecoveryMessage with empty dictionaries
            RecoveryMessageGenerator.GenerateWithEmptyDictionaries(outputDirectory);

            // 21. RecoveryMessage with null dictionaries
            RecoveryMessageGenerator.GenerateWithNullDictionaries(outputDirectory);

            // 22. RecoveryMessage with maximum entries
            RecoveryMessageGenerator.GenerateWithMaxEntries(outputDirectory);

            // ChangeView reason edge cases
            // 23. ChangeView with reason = 0 (timeout)
            ChangeViewGenerator.GenerateWithCustomReason(outputDirectory, Types.ChangeViewReason.Timeout);

            // 24. ChangeView with reason = 1 (change agreement)
            ChangeViewGenerator.GenerateWithCustomReason(outputDirectory, Types.ChangeViewReason.ChangeAgreement);

            // 25. ChangeView with reason = 2 (transaction not found)
            ChangeViewGenerator.GenerateWithCustomReason(outputDirectory, Types.ChangeViewReason.TxNotFound);

            // 26. ChangeView with invalid reason value
            ChangeViewGenerator.GenerateWithInvalidReason(outputDirectory);

            // Timestamp edge cases
            // 27. Commit with timestamp in the future
            CommitGenerator.GenerateWithFutureTimestamp(outputDirectory);

            // 28. PrepareRequest with timestamp in the past
            PrepareRequestGenerator.GenerateWithPastTimestamp(outputDirectory);

            // 29. Message with timestamp at ulong.MaxValue
            CommitGenerator.GenerateWithCustomTimestamp(outputDirectory, ulong.MaxValue);

            // 30. Message with timestamp at 0
            CommitGenerator.GenerateWithCustomTimestamp(outputDirectory, 0);

            // Block index edge cases
            // 31. Message with block index at uint.MaxValue
            CommitGenerator.GenerateWithCustomBlockIndex(outputDirectory, uint.MaxValue);

            // 32. Message with block index at 0
            CommitGenerator.GenerateWithCustomBlockIndex(outputDirectory, 0);

            // 33. Message with block index at 1
            CommitGenerator.GenerateWithCustomBlockIndex(outputDirectory, 1);

            // Byzantine behavior cases
            // 34. Multiple consecutive ChangeView messages for the same validator
            ChangeViewGenerator.GenerateConsecutiveViewChanges(outputDirectory);

            // 35. Byzantine behavior: conflicting PrepareRequests from the same validator
            PrepareRequestGenerator.GenerateConflictingRequests(outputDirectory);

            // 36. Byzantine behavior: commit for wrong view
            CommitGenerator.GenerateWithMismatchedView(outputDirectory);

            // 37. Byzantine behavior: different validators committing to different views
            GenerateMixedViewCommits(outputDirectory);

            // Network scenarios
            // 38. Network partition simulation: partial set of messages
            GenerateNetworkPartitionScenario(outputDirectory);

            // 39. Liveness test: sequence of messages leading to consensus
            GenerateLivenessTestSequence(outputDirectory);

            // 40. Recovery scenario: node coming back online
            GenerateRecoveryScenario(outputDirectory);

            // 41. View change scenario: primary failure
            GenerateViewChangeScenario(outputDirectory);

            // 42. Multiple view changes scenario
            GenerateMultipleViewChangesScenario(outputDirectory);

            // 43. Concurrent consensus rounds
            GenerateConcurrentConsensusRounds(outputDirectory);

            // 44. Malformed message sequences
            GenerateMalformedSequences(outputDirectory);

            // 45. Boundary testing for consensus thresholds
            GenerateConsensusThresholdTests(outputDirectory);
        }

        /// <summary>
        /// Generate a sequence of messages that simulates a network partition
        /// </summary>
        private static void GenerateNetworkPartitionScenario(string outputDirectory)
        {
            // Generate a scenario where only some validators can communicate
            // This tests the algorithm's ability to handle network partitions

            // First, generate a PrepareRequest from the primary
            PrepareRequestGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 0);

            // Then generate PrepareResponses from only a subset of validators (not enough for consensus)
            for (byte i = 1; i < 3; i++)
            {
                PrepareResponseGenerator.GenerateWithCustomValidatorIndex(outputDirectory, i);
            }

            // Generate ChangeView messages from the remaining validators
            for (byte i = 3; i < 7; i++)
            {
                ChangeViewGenerator.GenerateWithCustomValidatorIndex(outputDirectory, i);
            }

            // Save this as a special test case
            File.WriteAllText(Path.Combine(outputDirectory, "network_partition_scenario.txt"),
                "This directory contains a sequence of messages that simulate a network partition scenario.");
        }

        /// <summary>
        /// Generate a sequence of messages that tests liveness properties
        /// </summary>
        private static void GenerateLivenessTestSequence(string outputDirectory)
        {
            // Generate a complete sequence of messages that should lead to consensus
            // This tests the liveness property of the consensus algorithm

            // 1. PrepareRequest from primary (validator 0)
            PrepareRequestGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 0, "liveness_1_prepare_request.bin");

            // 2. PrepareResponses from other validators
            for (byte i = 1; i < 7; i++)
            {
                PrepareResponseGenerator.GenerateWithCustomValidatorIndex(outputDirectory, i, $"liveness_2_prepare_response_{i}.bin");
            }

            // 3. Commits from all validators
            for (byte i = 0; i < 7; i++)
            {
                CommitGenerator.GenerateWithCustomValidatorIndex(outputDirectory, i, $"liveness_3_commit_{i}.bin");
            }

            // Save this as a special test case
            File.WriteAllText(Path.Combine(outputDirectory, "liveness_test_sequence.txt"),
                "This directory contains a sequence of messages that should lead to consensus.");
        }

        /// <summary>
        /// Generate a scenario with mixed view commits (Byzantine behavior)
        /// </summary>
        private static void GenerateMixedViewCommits(string outputDirectory)
        {
            // Generate commits from different validators with different view numbers
            // This tests the algorithm's ability to detect Byzantine behavior

            // Generate commits for view 0
            for (byte i = 0; i < 3; i++)
            {
                CommitGenerator.GenerateWithCustomViewAndValidatorIndex(outputDirectory, 0, i, $"mixed_view_commit_v0_{i}.bin");
            }

            // Generate commits for view 1
            for (byte i = 3; i < 7; i++)
            {
                CommitGenerator.GenerateWithCustomViewAndValidatorIndex(outputDirectory, 1, i, $"mixed_view_commit_v1_{i}.bin");
            }

            // Save this as a special test case
            File.WriteAllText(Path.Combine(outputDirectory, "mixed_view_commits.txt"),
                "This directory contains commits from different validators with different view numbers (Byzantine behavior).");
        }

        /// <summary>
        /// Generate a recovery scenario
        /// </summary>
        private static void GenerateRecoveryScenario(string outputDirectory)
        {
            // Generate a scenario where a node needs to recover after being offline
            // This tests the recovery mechanism of the consensus algorithm

            // 1. RecoveryRequest from a validator that was offline
            RecoveryRequestGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 3, "recovery_1_request.bin");

            // 2. RecoveryMessage from the primary
            RecoveryMessageGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 0, "recovery_2_message.bin");

            // 3. PrepareResponse from the recovered validator
            PrepareResponseGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 3, "recovery_3_prepare_response.bin");

            // 4. Commit from the recovered validator
            CommitGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 3, "recovery_4_commit.bin");

            // Save this as a special test case
            File.WriteAllText(Path.Combine(outputDirectory, "recovery_scenario.txt"),
                "This directory contains a sequence of messages that simulate a node recovery scenario.");
        }

        /// <summary>
        /// Generate a view change scenario due to primary failure
        /// </summary>
        private static void GenerateViewChangeScenario(string outputDirectory)
        {
            // Generate a scenario where the primary fails and a view change occurs
            // This tests the view change mechanism of the consensus algorithm

            // 1. ChangeView messages from validators due to primary timeout
            for (byte i = 1; i < 7; i++)
            {
                ChangeViewGenerator.GenerateWithCustomValidatorIndex(outputDirectory, i, $"view_change_1_change_view_{i}.bin");
            }

            // 2. PrepareRequest from the new primary (validator 1) in view 1
            PrepareRequestGenerator.GenerateWithCustomViewAndValidatorIndex(outputDirectory, 1, 1, "view_change_2_prepare_request.bin");

            // 3. PrepareResponses from other validators in view 1
            for (byte i = 0; i < 7; i++)
            {
                if (i != 1) // Skip the new primary
                {
                    PrepareResponseGenerator.GenerateWithCustomViewAndValidatorIndex(outputDirectory, 1, i, $"view_change_3_prepare_response_{i}.bin");
                }
            }

            // 4. Commits from all validators in view 1
            for (byte i = 0; i < 7; i++)
            {
                CommitGenerator.GenerateWithCustomViewAndValidatorIndex(outputDirectory, 1, i, $"view_change_4_commit_{i}.bin");
            }

            // Save this as a special test case
            File.WriteAllText(Path.Combine(outputDirectory, "view_change_scenario.txt"),
                "This directory contains a sequence of messages that simulate a view change due to primary failure.");
        }

        /// <summary>
        /// Generate a scenario with multiple view changes
        /// </summary>
        private static void GenerateMultipleViewChangesScenario(string outputDirectory)
        {
            // Generate a scenario with multiple consecutive view changes
            // This tests the algorithm's ability to handle multiple view changes

            // View 0 to 1 change
            for (byte i = 0; i < 7; i++)
            {
                ChangeViewGenerator.GenerateWithCustomViewAndValidatorIndex(outputDirectory, 0, i, $"multi_view_1_change_0_to_1_{i}.bin");
            }

            // View 1 to 2 change
            for (byte i = 0; i < 7; i++)
            {
                ChangeViewGenerator.GenerateWithCustomViewAndValidatorIndex(outputDirectory, 1, i, $"multi_view_2_change_1_to_2_{i}.bin");
            }

            // View 2 to 3 change
            for (byte i = 0; i < 7; i++)
            {
                ChangeViewGenerator.GenerateWithCustomViewAndValidatorIndex(outputDirectory, 2, i, $"multi_view_3_change_2_to_3_{i}.bin");
            }

            // PrepareRequest from the new primary (validator 3) in view 3
            PrepareRequestGenerator.GenerateWithCustomViewAndValidatorIndex(outputDirectory, 3, 3, "multi_view_4_prepare_request.bin");

            // Save this as a special test case
            File.WriteAllText(Path.Combine(outputDirectory, "multiple_view_changes.txt"),
                "This directory contains a sequence of messages that simulate multiple consecutive view changes.");
        }

        /// <summary>
        /// Generate a scenario with concurrent consensus rounds
        /// </summary>
        private static void GenerateConcurrentConsensusRounds(string outputDirectory)
        {
            // Generate messages for two concurrent consensus rounds (different block indices)
            // This tests the algorithm's ability to handle concurrent consensus rounds

            // Block 100 consensus
            // PrepareRequest from primary for block 100
            PrepareRequestGenerator.GenerateWithCustomBlockIndex(outputDirectory, 100, "concurrent_1_block_100_prepare_request.bin");

            // Block 101 consensus
            // PrepareRequest from primary for block 101
            PrepareRequestGenerator.GenerateWithCustomBlockIndex(outputDirectory, 101, "concurrent_2_block_101_prepare_request.bin");

            // Mix of responses for both blocks
            for (byte i = 1; i < 7; i++)
            {
                if (i % 2 == 0)
                {
                    // Even validators respond to block 100
                    PrepareResponseGenerator.GenerateWithCustomBlockAndValidatorIndex(outputDirectory, 100, i, $"concurrent_3_block_100_prepare_response_{i}.bin");
                }
                else
                {
                    // Odd validators respond to block 101
                    PrepareResponseGenerator.GenerateWithCustomBlockAndValidatorIndex(outputDirectory, 101, i, $"concurrent_4_block_101_prepare_response_{i}.bin");
                }
            }

            // Save this as a special test case
            File.WriteAllText(Path.Combine(outputDirectory, "concurrent_consensus.txt"),
                "This directory contains messages for concurrent consensus rounds with different block indices.");
        }

        /// <summary>
        /// Generate malformed message sequences
        /// </summary>
        private static void GenerateMalformedSequences(string outputDirectory)
        {
            // Generate sequences of messages that are valid individually but invalid as a sequence
            // This tests the algorithm's ability to handle malformed sequences

            // 1. Commit without PrepareRequest
            CommitGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 0, "malformed_1_commit_without_prepare.bin");

            // 2. PrepareResponse without PrepareRequest
            PrepareResponseGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 1, "malformed_2_response_without_prepare.bin");

            // 3. PrepareRequest after Commit
            PrepareRequestGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 0, "malformed_3_prepare_after_commit.bin");

            // 4. ChangeView after Commit
            ChangeViewGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 2, "malformed_4_change_view_after_commit.bin");

            // 5. RecoveryRequest with incorrect block index
            RecoveryRequestGenerator.GenerateWithCustomBlockIndex(outputDirectory, 999, "malformed_5_recovery_wrong_block.bin");

            // Save this as a special test case
            File.WriteAllText(Path.Combine(outputDirectory, "malformed_sequences.txt"),
                "This directory contains sequences of messages that are valid individually but invalid as a sequence.");
        }

        /// <summary>
        /// Generate tests for consensus threshold boundaries
        /// </summary>
        private static void GenerateConsensusThresholdTests(string outputDirectory)
        {
            // Generate scenarios that test the boundaries of consensus thresholds
            // For 7 validators, M = 5 (2f+1 where f=2)

            // 1. Exactly M PrepareResponses (should reach consensus)
            PrepareRequestGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 0, "threshold_1_prepare_request.bin");
            for (byte i = 1; i < 6; i++) // 5 responses (validators 1-5)
            {
                PrepareResponseGenerator.GenerateWithCustomValidatorIndex(outputDirectory, i, $"threshold_2_prepare_response_{i}.bin");
            }

            // 2. M-1 PrepareResponses (should not reach consensus)
            PrepareRequestGenerator.GenerateWithCustomValidatorIndex(outputDirectory, 0, "threshold_3_prepare_request.bin");
            for (byte i = 1; i < 5; i++) // 4 responses (validators 1-4)
            {
                PrepareResponseGenerator.GenerateWithCustomValidatorIndex(outputDirectory, i, $"threshold_4_prepare_response_{i}.bin");
            }

            // 3. Exactly M Commits (should reach consensus)
            for (byte i = 0; i < 5; i++) // 5 commits (validators 0-4)
            {
                CommitGenerator.GenerateWithCustomValidatorIndex(outputDirectory, i, $"threshold_5_commit_{i}.bin");
            }

            // 4. M-1 Commits (should not reach consensus)
            for (byte i = 0; i < 4; i++) // 4 commits (validators 0-3)
            {
                CommitGenerator.GenerateWithCustomValidatorIndex(outputDirectory, i, $"threshold_6_commit_{i}.bin");
            }

            // Save this as a special test case
            File.WriteAllText(Path.Combine(outputDirectory, "consensus_thresholds.txt"),
                "This directory contains scenarios that test the boundaries of consensus thresholds.");
        }
    }
}
