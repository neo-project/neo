// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzConsensus.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using Neo.Wallets;
using SharpFuzz;
using System;
using System.IO;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Core
{
    /// <summary>
    /// Main fuzzing class for DBFT consensus
    /// </summary>
    public partial class FuzzConsensus
    {
        // Static fields for consensus context and system
        private static readonly NeoSystem _system;
        private static readonly Wallet _wallet;
        private static readonly Settings _settings;
        private static ConsensusContext _context;

        /// <summary>
        /// Fuzzing entry point - called by SharpFuzz
        /// </summary>
        /// <param name="stream">Input stream containing fuzzing data</param>
        public static void Fuzz(Stream stream)
        {
            // Always reset context fully for each input to ensure proper isolation
            InitializeContext();

            try
            {
                using var reader = new BinaryReader(stream);
                // Limit read size to prevent excessive memory usage
                var payloadBytes = reader.ReadBytes((int)Math.Min(stream.Length, 1024 * 10)); // Limit to 10KB

                if (payloadBytes.Length < 1)
                {
                    LogVerbose("Skipping empty input");
                    return; // Need at least category
                }

                var payload = new ExtensiblePayload();
                var memoryReader = new MemoryReader(payloadBytes);
                try
                {
                    ((ISerializable)payload).Deserialize(ref memoryReader);
                }
                catch (FormatException)
                {
                    LogVerbose("Skipping input with invalid payload format");
                    return;
                }
                catch (EndOfStreamException)
                {
                    LogVerbose("Skipping input with incomplete payload");
                    return;
                }
                catch (ArgumentOutOfRangeException)
                {
                    LogVerbose("Skipping input with out-of-range arguments in payload");
                    return;
                }

                // Basic validation of the extensible payload itself
                if (payload.Sender == null || payload.Data.IsEmpty)
                {
                    LogVerbose("Skipping input with null sender or empty data");
                    return;
                }

                ConsensusMessage message = null;
                try
                {
                    // Attempt to deserialize the inner consensus message based on category
                    if (Enum.TryParse<ConsensusMessageType>(payload.Category, true, out var messageType))
                    {
                        message = ConsensusMessage.DeserializeFrom(payload.Data.ToArray());
                        LogVerbose($"Successfully deserialized {messageType} message");
                    }
                    else
                    {
                        LogVerbose($"Skipping input with invalid category: {payload.Category}");
                        return;
                    }
                }
                catch (FormatException ex)
                {
                    LogVerbose($"Skipping input with format error in inner message: {ex.Message}");
                    return;
                }
                catch (EndOfStreamException)
                {
                    LogVerbose("Skipping input with incomplete inner message");
                    return;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    LogVerbose($"Skipping input with out-of-range arguments in inner message: {ex.Message}");
                    return;
                }

                if (message == null)
                {
                    LogVerbose("Skipping input that produced null message");
                    return;
                }

                // Verify message fields are within valid ranges
                if (!VerifyMessageBoundaries(message))
                {
                    LogVerbose("Skipping input with message fields outside valid boundaries");
                    return;
                }

                // Determine if we should test liveness by simulating a sequence of messages
                // Use a deterministic approach based on the hash of the payload
                int testType = payload.Hash.GetHashCode() % 50; // Use hash to determine test type

                if (testType == 0) // 2% of inputs will test normal consensus
                {
                    // Simulate a sequence of messages that should lead to consensus
                    try
                    {
                        SimulateConsensusSequence(message);
                        LogVerbose("Successfully simulated consensus sequence");
                    }
                    catch (InvalidOperationException ioe)
                    {
                        // This could indicate a liveness issue
                        LogWarning($"Liveness test failed: {ioe.Message}");
                        RecordInterestingTestCase(payloadBytes, $"LivenessIssue: {ioe.Message}");
                    }
                }
                else if (testType == 1) // 2% of inputs will test consensus with view changes
                {
                    // Simulate a sequence of messages with view changes
                    try
                    {
                        SimulateConsensusWithViewChanges(message);
                        LogVerbose("Successfully simulated consensus with view changes");
                    }
                    catch (InvalidOperationException ioe)
                    {
                        // This could indicate a liveness issue
                        LogWarning($"Liveness test with view changes failed: {ioe.Message}");
                        RecordInterestingTestCase(payloadBytes, $"LivenessIssueWithViewChanges: {ioe.Message}");
                    }
                }
                else if (testType == 2) // 2% of inputs will test Byzantine behavior with conflicting messages
                {
                    // Simulate Byzantine behavior with conflicting messages
                    try
                    {
                        SimulateByzantineBehavior(message);
                        LogVerbose("Successfully simulated Byzantine behavior");
                    }
                    catch (InvalidOperationException ioe)
                    {
                        // This could indicate a safety issue
                        LogWarning($"Byzantine behavior test failed: {ioe.Message}");
                        RecordInterestingTestCase(payloadBytes, $"ByzantineIssue: {ioe.Message}");
                    }
                }
                else if (testType == 3) // 2% of inputs will test network partitioning
                {
                    // Simulate network partitioning
                    try
                    {
                        SimulateNetworkPartition(message);
                        LogVerbose("Successfully simulated network partition");
                    }
                    catch (InvalidOperationException ioe)
                    {
                        // This could indicate a safety or liveness issue
                        LogWarning($"Network partition test failed: {ioe.Message}");
                        RecordInterestingTestCase(payloadBytes, $"NetworkPartitionIssue: {ioe.Message}");
                    }
                }
                else if (testType == 4) // 2% of inputs will test recovery mechanism
                {
                    // Simulate recovery process
                    try
                    {
                        SimulateRecoveryProcess(message);
                        LogVerbose("Successfully simulated recovery process");
                    }
                    catch (InvalidOperationException ioe)
                    {
                        // This could indicate a recovery issue
                        LogWarning($"Recovery test failed: {ioe.Message}");
                        RecordInterestingTestCase(payloadBytes, $"RecoveryIssue: {ioe.Message}");
                    }
                }
                else
                {
                    // Standard message processing for individual messages
                    try
                    {
                        SimulateMessageProcessing(payload, message);
                        LogVerbose($"Successfully processed {message.GetType().Name} message");
                    }
                    catch (InvalidOperationException ioe)
                    {
                        // This is an expected exception during fuzzing - it indicates we found a logic issue
                        LogWarning($"Simulated processing error: {ioe.Message}");

                        // Don't rethrow - this is an expected failure case we want to detect
                        // Record this as an interesting test case but continue fuzzing
                        RecordInterestingTestCase(payloadBytes, $"InvalidOperation: {ioe.Message}");
                    }
                    catch (ArgumentException ae)
                    {
                        LogWarning($"Simulated processing argument error: {ae.Message}");
                        RecordInterestingTestCase(payloadBytes, $"ArgumentException: {ae.Message}");
                    }

                    // Check Invariants after processing
                    try
                    {
                        CheckInvariants();
                    }
                    catch (InvalidOperationException ioe)
                    {
                        // Invariant violation - this is a serious issue we want to detect
                        LogError($"Invariant violation: {ioe.Message}");
                        RecordInterestingTestCase(payloadBytes, $"InvariantViolation: {ioe.Message}");

                        // Rethrow to signal crash to SharpFuzz - invariant violations are critical
                        throw;
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // Ignore, fuzzing might generate incomplete data
                LogVerbose("Encountered end of stream exception");
            }
            catch (Exception ex)
            {
                // Catch unexpected exceptions that might indicate bugs
                LogError($"Fuzzing caused unexpected exception: {ex}");

                // Rethrow to signal crash to SharpFuzz
                throw;
            }
        }
    }
}
