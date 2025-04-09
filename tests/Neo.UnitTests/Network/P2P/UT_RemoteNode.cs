// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RemoteNode.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.IO;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO.Caching;
using Neo.Network.P2P;
using Neo.Network.P2P.Capabilities;
using Neo.Network.P2P.Payloads;
using System.Net;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using Akka.TestKit;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.Network.P2P
{
    [TestClass]
    public class UT_RemoteNode : TestKit
    {
        private static NeoSystem testBlockchain;

        public UT_RemoteNode()
            : base($"remote-node-mailbox {{ mailbox-type: \"{typeof(RemoteNodeMailbox).AssemblyQualifiedName}\" }}")
        {
        }

        [ClassInitialize]
        public static void TestSetup(TestContext ctx)
        {
            testBlockchain = TestBlockchain.TheNeoSystem;
        }

        [TestMethod]
        public void RemoteNode_Test_Abort_DifferentNetwork()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, new LocalNode(testBlockchain), connectionTestProbe, null, null));

            var msg = Message.Create(MessageCommand.Version, new VersionPayload
            {
                UserAgent = "".PadLeft(1024, '0'),
                Nonce = 1,
                Network = 2,
                Timestamp = 5,
                Version = 6,
                Capabilities = new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                }
            });

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, new Tcp.Received((ByteString)msg.ToArray()));

            connectionTestProbe.ExpectMsg<Tcp.Abort>();
        }

        [TestMethod]
        public void RemoteNode_Test_Accept_IfSameNetwork()
        {
            var connectionTestProbe = CreateTestProbe();
            var remoteNodeActor = ActorOfAsTestActorRef(() => new RemoteNode(testBlockchain, new LocalNode(testBlockchain), connectionTestProbe, new IPEndPoint(IPAddress.Parse("192.168.1.2"), 8080), new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8080)));

            var msg = Message.Create(MessageCommand.Version, new VersionPayload()
            {
                UserAgent = "Unit Test".PadLeft(1024, '0'),
                Nonce = 1,
                Network = TestProtocolSettings.Default.Network,
                Timestamp = 5,
                Version = 6,
                Capabilities = new NodeCapability[]
                {
                    new ServerCapability(NodeCapabilityType.TcpServer, 25)
                }
            });

            var testProbe = CreateTestProbe();
            testProbe.Send(remoteNodeActor, new Tcp.Received((ByteString)msg.ToArray()));

            var verackMessage = connectionTestProbe.ExpectMsg<Tcp.Write>();

            //Verack
            Assert.AreEqual(3, verackMessage.Data.Count);
        }

        /// <summary>
        /// This test verifies the large capacity of the HashSetCache fields
        /// (knownHashes and sentHashes) in RemoteNode BEFORE the optimization
        /// introduced in PR #3874. This serves as a baseline to demonstrate
        /// the potential high memory usage targeted by the PR.
        /// It uses direct access to internal fields.
        /// </summary>
        [TestMethod]
        public void RemoteNode_Test_PreFix_HashSetCache_Capacity()
        {
            // Arrange
            var connectionTestProbe = CreateTestProbe();
            // Use ActorOfAsTestActorRef to get access to the underlying actor instance
            var remoteNodeActorRef = ActorOfAsTestActorRef<RemoteNode>(() => new RemoteNode(testBlockchain, new LocalNode(testBlockchain), connectionTestProbe, null, null));
            var remoteNode = remoteNodeActorRef.UnderlyingActor;

            // Access the internal HashSetCache fields directly
            var knownHashesCache = remoteNode.knownHashes;
            var sentHashesCache = remoteNode.sentHashes;

            Assert.IsNotNull(knownHashesCache, "knownHashes cache is null.");
            Assert.IsNotNull(sentHashesCache, "sentHashes cache is null.");

            // Access the internal capacity fields within HashSetCache directly
            var knownBucketCapacity = knownHashesCache._bucketCapacity;
            var knownMaxBucketCount = knownHashesCache._maxBucketCount;
            var sentBucketCapacity = sentHashesCache._bucketCapacity;
            var sentMaxBucketCount = sentHashesCache._maxBucketCount;

            // Assert
            // Verify the cache capacities match the values *before* PR #3874 fix.
            // The original code initialized them via the constructor which uses
            // system.MemPool.Capacity * 2 / 5. We need to know the default MemPool Capacity.
            // Assuming the MemPool.Capacity * 2 / 5 resulted in 20_000 for this test setup's NeoSystem.
            // The default maxBucketCount is 10.
            int expectedBucketCapacity = testBlockchain.MemPool.Capacity * 2 / 5;

            Assert.AreEqual(expectedBucketCapacity, knownBucketCapacity, "KnownHashes _bucketCapacity does not match calculated pre-fix value.");
            Assert.AreEqual(10, knownMaxBucketCount, "KnownHashes _maxBucketCount does not match default value.");
            Assert.AreEqual(expectedBucketCapacity, sentBucketCapacity, "SentHashes _bucketCapacity does not match calculated pre-fix value.");
            Assert.AreEqual(10, sentMaxBucketCount, "SentHashes _maxBucketCount does not match default value.");

            // Total potential capacity check (as described in the PR)
            Assert.AreEqual(expectedBucketCapacity * 10, knownBucketCapacity * knownMaxBucketCount, "KnownHashes total potential capacity mismatch.");
            Assert.AreEqual(expectedBucketCapacity * 10, sentBucketCapacity * sentMaxBucketCount, "SentHashes total potential capacity mismatch.");
        }

        /// <summary>
        /// Simulates a scenario with multiple remote nodes, each receiving
        /// inventory messages, to demonstrate the potential for high memory usage
        /// due to large HashSetCache populations *before* the fix in PR #3874.
        /// This test verifies that the caches fill up as expected under load,
        /// implying high memory potential, but does not measure actual memory/CPU.
        /// </summary>
        [TestMethod]
        public void RemoteNode_Test_Simulate_High_Memory_Scenario()
        {
            // Log initial memory
            Console.WriteLine($"\n--- Starting {nameof(RemoteNode_Test_Simulate_High_Memory_Scenario)} ---");
            long initialMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Initial Memory Usage: {initialMemory / 1024.0 / 1024.0:F2} MB");

            // Arrange
            const int NodeCount = 40;
            var remoteNodes = new List<RemoteNode>();
            var probes = new List<TestProbe>();
            const BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            // 1. Initialize multiple nodes
            for (var i = 0; i < NodeCount; i++)
            {
                var connectionTestProbe = CreateTestProbe();
                probes.Add(connectionTestProbe);
                var remoteEndpoint = new IPEndPoint(IPAddress.Parse($"192.168.1.{10 + i}"), 8080 + i);
                var localEndpoint = new IPEndPoint(IPAddress.Parse("192.168.0.1"), 8080);
                var remoteNodeActorRef = ActorOfAsTestActorRef<RemoteNode>(() => new RemoteNode(testBlockchain, new LocalNode(testBlockchain), connectionTestProbe.Ref, remoteEndpoint, localEndpoint));
                remoteNodes.Add(remoteNodeActorRef.UnderlyingActor);
            }

            // 2. Simulate handshake for each node (simplified - directly set state)
            foreach (var node in remoteNodes)
            {
                var versionPayload = new VersionPayload
                {
                    Network = TestProtocolSettings.Default.Network,
                    Nonce = 1,
                    UserAgent = "SimulatingNode",
                    Timestamp = DateTime.UtcNow.ToTimestamp(),
                    Capabilities = [new FullNodeCapability(0)]
                };
                // Use reflection for property with private setter
                node.GetType().GetProperty("Version").SetValue(node, versionPayload);
                // Use reflection for private field
                node.GetType().GetField("verack", BindingFlags).SetValue(node, true);

                Assert.IsNotNull(node.knownHashes, "knownHashes is null post-init");
                Assert.IsNotNull(node.sentHashes, "sentHashes is null post-init");
            }

            // 3. Generate Hashes & Simulate Messages
            // Calculate max capacity per cache based on the first node's configuration
            var bucketCapacity = remoteNodes[0].knownHashes._bucketCapacity;
            var maxBucketCount = remoteNodes[0].knownHashes._maxBucketCount;
            var maxCapacityPerCache = bucketCapacity * maxBucketCount;
            Console.WriteLine($"Targeting Max Capacity Per Cache: {maxCapacityPerCache} (Bucket Capacity: {bucketCapacity}, Max Buckets: {maxBucketCount})");

            var allHashes = new List<UInt256>();
            // Generate enough unique hashes for BOTH caches for all nodes
            var totalHashesNeeded = NodeCount * maxCapacityPerCache * 2;
            Console.WriteLine($"Generating {totalHashesNeeded} unique hashes...");
            for (var i = 0; i < totalHashesNeeded; i++)
            {
                var data = new byte[32];
                System.Security.Cryptography.RandomNumberGenerator.Fill(data);
                allHashes.Add(new UInt256(new ReadOnlySpan<byte>(data)));
            }
            Console.WriteLine("Hash generation complete.");

            // Prepare reflection MethodInfos once
            var onMessageMethod = typeof(RemoteNode).GetMethod("OnMessage", BindingFlags);
            var onInventoryReceivedMethod = typeof(RemoteNode).GetMethod("OnInventoryReceived", BindingFlags);
            var txHashField = typeof(Transaction).GetField("_hash", BindingFlags); // Assumes Transaction has private field _hash

            Assert.IsNotNull(onMessageMethod, "OnMessage method not found.");
            Assert.IsNotNull(onInventoryReceivedMethod, "OnInventoryReceived method not found.");
            Assert.IsNotNull(txHashField, "Transaction._hash field not found. Ensure this field exists.");

            for (var i = 0; i < NodeCount; i++)
            {
                var node = remoteNodes[i];
                // Get distinct hashes for known and sent for this node
                var hashesForKnown = allHashes.Skip(i * maxCapacityPerCache * 2).Take(maxCapacityPerCache).ToArray();
                var hashesForSent = allHashes.Skip(i * maxCapacityPerCache * 2 + maxCapacityPerCache).Take(maxCapacityPerCache).ToArray();

                // --- Simulate knownHashes population ---
                // 3a. Simulate receiving InvPayload (Type=TX)
                var invPayload = InvPayload.Create(InventoryType.TX, hashesForKnown); // Use specific hashes for known
                var invMessage = Message.Create(MessageCommand.Inv, invPayload);
                onMessageMethod.Invoke(node, [invMessage]); // Calls OnInvMessageReceived

                // 3b. Simulate receiving the actual Transactions via OnInventoryReceived
                foreach (var hash in hashesForKnown)
                {
                    // Create minimal dummy TX
                    var currentHeight = NativeContract.Ledger.CurrentIndex(testBlockchain.StoreView);
                    var tx = new Transaction
                    {
                        Nonce = (uint)Random.Shared.Next(),
                        SystemFee = 1,
                        NetworkFee = 1,
                        ValidUntilBlock = currentHeight + 100,
                        Attributes = [],
                        Script = new byte[] { 0x01 },
                        Signers = [new Signer { Account = UInt160.Zero }],
                        Witnesses = [new Witness { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }
                        ]
                    };
                    // Set the hash via reflection
                    txHashField.SetValue(tx, hash);
                    // Call OnInventoryReceived to populate knownHashes
                    onInventoryReceivedMethod.Invoke(node, [tx]);
                }

                // --- Simulate sentHashes population ---
                // 3c. Simulate receiving GetData (Type=TX) with DISTINCT hashes
                var getDataPayload = InvPayload.Create(InventoryType.TX, hashesForSent); // Use specific hashes for sent
                var getDataMessage = Message.Create(MessageCommand.GetData, getDataPayload);
                onMessageMethod.Invoke(node, [getDataMessage]); // Calls OnGetDataMessageReceived

                if ((i + 1) % 10 == 0) Console.WriteLine($"Populated caches for node {i + 1}/{NodeCount}");
            }

            // Log memory after population
            var memoryAfterPopulation = GC.GetTotalMemory(true);
            Console.WriteLine($"Memory Usage After Cache Population: {memoryAfterPopulation / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Memory Increase due to Simulation: {(memoryAfterPopulation - initialMemory) / 1024.0 / 1024.0:F2} MB");

            // 4. Assert Cache Counts are high (should reach max capacity)
            var expectedKnownCount = maxCapacityPerCache;
            var expectedSentCount = maxCapacityPerCache;

            for (var i = 0; i < NodeCount; i++)
            {
                var node = remoteNodes[i];
                Assert.AreEqual(expectedKnownCount, node.knownHashes.Count, $"Node {i} knownHashes count mismatch.");
                Assert.AreEqual(expectedSentCount, node.sentHashes.Count, $"Node {i} sentHashes count mismatch.");

                // Optionally: Simulate lookups (use first hash from each set)
                Assert.IsTrue(node.knownHashes.Contains(allHashes[i * maxCapacityPerCache * 2]), "Lookup test known");
                Assert.IsTrue(node.sentHashes.Contains(allHashes[i * maxCapacityPerCache * 2 + maxCapacityPerCache]), "Lookup test sent");
                Assert.IsFalse(node.knownHashes.Contains(new UInt256()), "Negative lookup test");
            }
        }
    }
}
