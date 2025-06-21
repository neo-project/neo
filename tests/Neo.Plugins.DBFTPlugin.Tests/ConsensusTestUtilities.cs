// Copyright (C) 2015-2025 The Neo Project.
//
// ConsensusTestUtilities.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.TestKit;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.DBFTPlugin.Messages;
using Neo.Plugins.DBFTPlugin.Types;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    /// <summary>
    /// Helper class for consensus testing with message verification and state tracking.
    ///
    /// Proper consensus testing approach:
    /// 1. Send PrepareRequest to consensus services
    /// 2. Wait for natural PrepareResponse from backup validators
    /// 3. Wait for natural Commit messages from all validators
    ///
    /// This tests actual consensus logic flow rather than just message passing.
    /// </summary>
    public class ConsensusTestUtilities
    {
        private readonly TestProbe localNodeProbe;
        private readonly List<ExtensiblePayload> sentMessages;
        private readonly Dictionary<ConsensusMessageType, int> messageTypeCounts;
        private readonly Dictionary<IActorRef, TestProbe> actorProbes;

        public ConsensusTestUtilities(TestProbe localNodeProbe)
        {
            this.localNodeProbe = localNodeProbe;
            sentMessages = new List<ExtensiblePayload>();
            messageTypeCounts = new Dictionary<ConsensusMessageType, int>();
            actorProbes = new Dictionary<IActorRef, TestProbe>();
        }

        /// <summary>
        /// Creates a properly formatted consensus payload
        /// </summary>
        public ExtensiblePayload CreateConsensusPayload(ConsensusMessage message, int validatorIndex, uint blockIndex = 1, byte viewNumber = 0)
        {
            message.BlockIndex = blockIndex;
            message.ValidatorIndex = (byte)validatorIndex;
            message.ViewNumber = viewNumber;

            var payload = new ExtensiblePayload
            {
                Category = "dBFT",
                ValidBlockStart = 0,
                ValidBlockEnd = blockIndex,
                Sender = Contract.GetBFTAddress(MockProtocolSettings.Default.StandbyValidators),
                Data = message.ToArray(),
                Witness = new Witness
                {
                    InvocationScript = ReadOnlyMemory<byte>.Empty,
                    VerificationScript = new[] { (byte)OpCode.PUSH1 }
                }
            };

            // Track the message
            sentMessages.Add(payload);
            if (!messageTypeCounts.ContainsKey(message.Type))
                messageTypeCounts[message.Type] = 0;
            messageTypeCounts[message.Type]++;

            return payload;
        }

        /// <summary>
        /// Creates a PrepareRequest message
        /// </summary>
        public PrepareRequest CreatePrepareRequest(UInt256 prevHash = null, UInt256[] transactionHashes = null, ulong nonce = 0)
        {
            return new PrepareRequest
            {
                Version = 0,
                PrevHash = prevHash ?? UInt256.Zero,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Nonce = nonce,
                TransactionHashes = transactionHashes ?? Array.Empty<UInt256>()
            };
        }

        /// <summary>
        /// Creates a PrepareResponse message
        /// </summary>
        public PrepareResponse CreatePrepareResponse(UInt256 preparationHash = null)
        {
            return new PrepareResponse
            {
                PreparationHash = preparationHash ?? UInt256.Zero
            };
        }

        /// <summary>
        /// Creates a Commit message
        /// </summary>
        public Commit CreateCommit(byte[] signature = null)
        {
            return new Commit
            {
                Signature = signature ?? new byte[64] // Fake signature for testing
            };
        }

        /// <summary>
        /// Creates a ChangeView message
        /// </summary>
        public ChangeView CreateChangeView(ChangeViewReason reason = ChangeViewReason.Timeout)
        {
            return new ChangeView
            {
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Reason = reason
            };
        }

        /// <summary>
        /// Creates a RecoveryRequest message
        /// </summary>
        public RecoveryRequest CreateRecoveryRequest()
        {
            return new RecoveryRequest
            {
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        /// <summary>
        /// Sets up message interception for consensus services
        /// </summary>
        public void SetupMessageInterception(IActorRef[] consensusServices)
        {
            foreach (var service in consensusServices)
            {
                actorProbes[service] = localNodeProbe;
            }
        }

        /// <summary>
        /// Waits for consensus services to naturally send messages of a specific type
        /// </summary>
        public async Task<List<ExtensiblePayload>> WaitForConsensusMessages(
            IActorRef[] consensusServices,
            ConsensusMessageType expectedMessageType,
            int expectedCount,
            TimeSpan timeout)
        {
            var receivedMessages = new List<ExtensiblePayload>();
            var endTime = DateTime.UtcNow.Add(timeout);

            while (receivedMessages.Count < expectedCount && DateTime.UtcNow < endTime)
            {
                try
                {
                    var message = localNodeProbe.ReceiveOne(TimeSpan.FromMilliseconds(100));

                    if (message is ExtensiblePayload payload)
                    {
                        try
                        {
                            var consensusMessage = ConsensusMessage.DeserializeFrom(payload.Data);
                            if (consensusMessage.Type == expectedMessageType)
                            {
                                receivedMessages.Add(payload);
                                sentMessages.Add(payload);

                                if (!messageTypeCounts.ContainsKey(expectedMessageType))
                                    messageTypeCounts[expectedMessageType] = 0;
                                messageTypeCounts[expectedMessageType]++;
                            }
                        }
                        catch
                        {
                            // Ignore malformed messages
                        }
                    }
                }
                catch
                {
                    await Task.Delay(10);
                }
            }

            return receivedMessages;
        }

        /// <summary>
        /// Sends a message to multiple consensus services
        /// </summary>
        public void SendToAll(ExtensiblePayload payload, IActorRef[] consensusServices)
        {
            foreach (var service in consensusServices)
            {
                service.Tell(payload);
            }
        }

        /// <summary>
        /// Sends a message to specific consensus services
        /// </summary>
        public void SendToValidators(ExtensiblePayload payload, IActorRef[] consensusServices, int[] validatorIndices)
        {
            foreach (var index in validatorIndices)
            {
                if (index >= 0 && index < consensusServices.Length)
                {
                    consensusServices[index].Tell(payload);
                }
            }
        }

        /// <summary>
        /// Simulates a complete consensus round with proper message flow
        /// </summary>
        public async Task SimulateCompleteConsensusRoundAsync(IActorRef[] consensusServices, uint blockIndex = 1, UInt256[] transactions = null)
        {
            var validatorCount = consensusServices.Length;
            var primaryIndex = (int)(blockIndex % (uint)validatorCount);

            // Primary sends PrepareRequest
            var prepareRequest = CreatePrepareRequest(transactionHashes: transactions);
            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, primaryIndex, blockIndex);
            SendToAll(prepareRequestPayload, consensusServices);

            // Wait for backup validators to naturally send PrepareResponse
            var expectedPrepareResponses = validatorCount - 1;
            var prepareResponses = await WaitForConsensusMessages(
                consensusServices,
                ConsensusMessageType.PrepareResponse,
                expectedPrepareResponses,
                TimeSpan.FromSeconds(5));

            // Wait for all validators to naturally send Commit messages
            var expectedCommits = validatorCount;
            var commits = await WaitForConsensusMessages(
                consensusServices,
                ConsensusMessageType.Commit,
                expectedCommits,
                TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Simulates consensus with proper message flow and TestProbe monitoring
        /// </summary>
        public void SimulateConsensusWithProperFlow(IActorRef[] consensusServices, TestProbe testProbe, uint blockIndex = 1)
        {
            var validatorCount = consensusServices.Length;
            var primaryIndex = (int)(blockIndex % (uint)validatorCount);

            // Primary sends PrepareRequest
            var prepareRequest = CreatePrepareRequest();
            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, primaryIndex, blockIndex);
            SendToAll(prepareRequestPayload, consensusServices);

            // Wait for backup validators to naturally trigger PrepareResponse
            // Test should monitor consensus services for natural message flow
        }

        /// <summary>
        /// Simulates a complete consensus round (legacy synchronous version)
        /// </summary>
        [Obsolete("Use SimulateCompleteConsensusRoundAsync for proper message flow testing")]
        public void SimulateCompleteConsensusRound(IActorRef[] consensusServices, uint blockIndex = 1, UInt256[] transactions = null)
        {
            var validatorCount = consensusServices.Length;
            var primaryIndex = (int)(blockIndex % (uint)validatorCount);

            // Primary sends PrepareRequest
            var prepareRequest = CreatePrepareRequest(transactionHashes: transactions);
            var prepareRequestPayload = CreateConsensusPayload(prepareRequest, primaryIndex, blockIndex);
            SendToAll(prepareRequestPayload, consensusServices);

            // Backup validators send PrepareResponse (immediate - not realistic)
            for (int i = 0; i < validatorCount; i++)
            {
                if (i != primaryIndex)
                {
                    var prepareResponse = CreatePrepareResponse();
                    var responsePayload = CreateConsensusPayload(prepareResponse, i, blockIndex);
                    SendToAll(responsePayload, consensusServices);
                }
            }

            // All validators send Commit (immediate - not realistic)
            for (int i = 0; i < validatorCount; i++)
            {
                var commit = CreateCommit();
                var commitPayload = CreateConsensusPayload(commit, i, blockIndex);
                SendToAll(commitPayload, consensusServices);
            }
        }

        /// <summary>
        /// Simulates a view change scenario
        /// </summary>
        public void SimulateViewChange(IActorRef[] consensusServices, int[] initiatingValidators, byte newViewNumber, ChangeViewReason reason = ChangeViewReason.Timeout)
        {
            foreach (var validatorIndex in initiatingValidators)
            {
                var changeView = CreateChangeView(reason);
                var changeViewPayload = CreateConsensusPayload(changeView, validatorIndex, viewNumber: newViewNumber);
                SendToAll(changeViewPayload, consensusServices);
            }
        }

        /// <summary>
        /// Simulates Byzantine behavior by sending conflicting messages
        /// </summary>
        public void SimulateByzantineBehavior(IActorRef[] consensusServices, int byzantineValidatorIndex, uint blockIndex = 1)
        {
            // Send conflicting PrepareResponse messages
            var response1 = CreatePrepareResponse(UInt256.Parse("0x1111111111111111111111111111111111111111111111111111111111111111"));
            var response2 = CreatePrepareResponse(UInt256.Parse("0x2222222222222222222222222222222222222222222222222222222222222222"));

            var payload1 = CreateConsensusPayload(response1, byzantineValidatorIndex, blockIndex);
            var payload2 = CreateConsensusPayload(response2, byzantineValidatorIndex, blockIndex);

            // Send different messages to different validators
            var halfCount = consensusServices.Length / 2;
            SendToValidators(payload1, consensusServices, Enumerable.Range(0, halfCount).ToArray());
            SendToValidators(payload2, consensusServices, Enumerable.Range(halfCount, consensusServices.Length - halfCount).ToArray());
        }

        /// <summary>
        /// Gets the count of sent messages by type
        /// </summary>
        public int GetMessageCount(ConsensusMessageType messageType)
        {
            return messageTypeCounts.TryGetValue(messageType, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets all sent messages
        /// </summary>
        public IReadOnlyList<ExtensiblePayload> GetSentMessages()
        {
            return sentMessages.AsReadOnly();
        }

        /// <summary>
        /// Gets sent messages of a specific type
        /// </summary>
        public IEnumerable<ExtensiblePayload> GetMessagesByType(ConsensusMessageType messageType)
        {
            return sentMessages.Where(payload =>
            {
                try
                {
                    var message = ConsensusMessage.DeserializeFrom(payload.Data);
                    return message.Type == messageType;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Clears all tracked messages
        /// </summary>
        public void ClearMessages()
        {
            sentMessages.Clear();
            messageTypeCounts.Clear();
        }

        /// <summary>
        /// Verifies that the expected consensus flow occurred
        /// </summary>
        public bool VerifyConsensusFlow(int expectedValidatorCount, bool shouldHaveCommits = true)
        {
            var prepareRequestCount = GetMessageCount(ConsensusMessageType.PrepareRequest);
            var prepareResponseCount = GetMessageCount(ConsensusMessageType.PrepareResponse);
            var commitCount = GetMessageCount(ConsensusMessageType.Commit);

            // Basic flow verification
            var hasValidFlow = prepareRequestCount > 0 &&
                              prepareResponseCount >= (expectedValidatorCount - 1); // Backup validators respond

            if (shouldHaveCommits)
            {
                hasValidFlow = hasValidFlow && commitCount >= expectedValidatorCount;
            }

            return hasValidFlow;
        }

        /// <summary>
        /// Creates multiple transaction hashes for testing
        /// </summary>
        public static UInt256[] CreateTestTransactions(int count)
        {
            var transactions = new UInt256[count];
            for (int i = 0; i < count; i++)
            {
                var txBytes = new byte[32];
                BitConverter.GetBytes(i).CopyTo(txBytes, 0);
                transactions[i] = new UInt256(txBytes);
            }
            return transactions;
        }
    }
}
