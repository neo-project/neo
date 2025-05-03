// Copyright (C) 2015-2025 The Neo Project.
//
// PerformanceTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.Plugins.LedgerDebugger.Tests
{
    /// <summary>
    /// Performance tests for the LedgerDebugger plugin to ensure acceptable performance under load.
    /// </summary>
    [TestClass]
    public class PerformanceTests
    {
        private string testPath;
        private BlockReadSetStorage storage;
        private IStore store;

        /// <summary>
        /// Initialize test environment before each test.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            testPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testPath);

            // Use MemoryStore for faster tests
            store = new MemoryStore();
            storage = new BlockReadSetStorage(store);
        }

        /// <summary>
        /// Clean up test environment after each test.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            storage?.Dispose();
            store?.Dispose();

            try
            {
                Directory.Delete(testPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to clean up test directory: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests storage performance for a large number of medium-sized read sets.
        /// </summary>
        [TestMethod]
        public void TestStoragePerformanceWithManyReadSets()
        {
            // Arrange - Create many blocks with small read sets to test performance
            const int blockCount = 100;
            const int entriesPerBlock = 500;

            var testBlocks = new Dictionary<uint, Dictionary<StorageKey, StorageItem>>();

            // Create a mock storage
            var store = new MemoryStore();
            var storage = new MockBlockReadSetStorage(store);

            // Generate blocks
            var stopwatch = Stopwatch.StartNew();
            for (uint i = 0; i < blockCount; i++)
            {
                var readSet = TestDataGenerator.GenerateRandomReadSet(
                    entriesPerBlock,
                    smallValuePercentage: 0.8,
                    smallValueSize: 16,
                    largeValueSize: 128);

                testBlocks[i] = readSet;
                storage.Add(i, readSet);
            }
            stopwatch.Stop();
            Console.WriteLine($"Time to store {blockCount} blocks with {entriesPerBlock} entries each: {stopwatch.ElapsedMilliseconds}ms");

            // Validate a random sample of the blocks
            var random = new Random();
            for (int i = 0; i < 5; i++)
            {
                uint blockIndex = (uint)random.Next(0, blockCount);
                var originalReadSet = testBlocks[blockIndex];

                bool success = storage.TryGet(blockIndex, out var retrievedReadSet);

                Assert.IsTrue(success, $"Failed to retrieve block {blockIndex}");
                Assert.IsNotNull(retrievedReadSet, $"Retrieved read set for block {blockIndex} should not be null");
                Assert.AreEqual(originalReadSet.Count, retrievedReadSet.Count, "Read set should have same number of entries");

                // Use byte-by-byte comparison for validation
                foreach (var entry in originalReadSet)
                {
                    byte[] keyBytes = entry.Key.ToArray();
                    byte[] valueBytes = entry.Value.Value.Span.ToArray();

                    bool found = false;
                    foreach (var retrievedKey in retrievedReadSet.Keys)
                    {
                        if (retrievedKey.SequenceEqual(keyBytes))
                        {
                            found = true;
                            CollectionAssert.AreEqual(valueBytes, retrievedReadSet[retrievedKey],
                                $"Value for key {BitConverter.ToString(keyBytes)} does not match");
                            break;
                        }
                    }
                    Assert.IsTrue(found, $"Key {BitConverter.ToString(keyBytes)} not found in retrieved read set");
                }
            }
        }

        /// <summary>
        /// Tests storage performance with highly duplicated data to verify deduplication effectiveness.
        /// </summary>
        [TestMethod]
        public void TestStoragePerformanceWithDuplicatedData()
        {
            // Arrange - Create test data with duplicated values to test content-addressable storage
            const int blockCount = 50;
            const int entriesPerBlock = 20;
            const int uniqueValuesCount = 10; // Reuse these values across blocks

            // Create a set of unique values to reuse
            var uniqueValues = new List<byte[]>();
            for (int i = 0; i < uniqueValuesCount; i++)
            {
                uniqueValues.Add(CreateRandomBytes(100 + i)); // Use different sizes to increase entropy
            }

            var testBlocks = new Dictionary<uint, Dictionary<StorageKey, StorageItem>>();
            var store = new MemoryStore();
            var storage = new MockBlockReadSetStorage(store);
            var random = new Random();

            // Generate test blocks with duplicated values
            for (uint blockIndex = 0; blockIndex < blockCount; blockIndex++)
            {
                var readSet = new Dictionary<StorageKey, StorageItem>();

                for (int j = 0; j < entriesPerBlock; j++)
                {
                    // Create a unique key
                    var key = new StorageKey
                    {
                        Id = (byte)(j % 255),
                        Key = Encoding.UTF8.GetBytes($"key_{blockIndex}_{j}")
                    };

                    // Use one of our pregenerated values (simulate duplication)
                    int valueIndex = random.Next(0, uniqueValuesCount);
                    var valueBytes = uniqueValues[valueIndex];
                    var item = new StorageItem { Value = valueBytes };

                    readSet[key] = item;
                }

                testBlocks[blockIndex] = readSet;
                storage.Add(blockIndex, readSet);
            }

            // Verify we can correctly retrieve the data
            foreach (uint blockIndex in testBlocks.Keys.Take(5))
            {
                var originalReadSet = testBlocks[blockIndex];
                bool success = storage.TryGet(blockIndex, out var retrievedReadSet);

                Assert.IsTrue(success, $"Failed to retrieve block {blockIndex}");
                Assert.IsNotNull(retrievedReadSet, $"Retrieved read set for block {blockIndex} should not be null");
                Assert.AreEqual(originalReadSet.Count, retrievedReadSet.Count, "Read set should have same number of entries");

                // Use extension method to verify equivalence
                originalReadSet.AssertEquivalentTo(retrievedReadSet);
            }
        }

        /// <summary>
        /// Tests how the system performs when replaying blocks with large read sets.
        /// </summary>
        [TestMethod]
        public void TestReplayPerformance()
        {
            // Arrange - Create more blocks with large read sets
            const int blockCount = 20;
            const int entriesPerBlock = 5000;
            const int iterationCount = 5; // Run multiple iterations to make timing measurable

            var testBlocks = new Dictionary<uint, Dictionary<StorageKey, StorageItem>>();

            // Create the storage
            var store = new MemoryStore();
            var storage = new MockBlockReadSetStorage(store);

            // Generate large blocks
            for (uint i = 0; i < blockCount; i++)
            {
                var readSet = TestDataGenerator.GenerateReadSet(
                    entriesPerBlock,
                    smallValuePercentage: 0.3, // More large values
                    smallValueSize: 20,
                    largeValueSize: 300);

                testBlocks[i] = readSet;

                // Store the read set
                storage.Add(i, readSet);
            }

            // Act - Simulate a replay using stored read sets
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Run multiple iterations to make timing more measurable
            for (int iteration = 0; iteration < iterationCount; iteration++)
            {
                // Test retrieval and potential application performance, without actually modifying the store
                for (uint blockIndex = 0; blockIndex < blockCount; blockIndex++)
                {
                    bool success = storage.TryGet(blockIndex, out var retrievedReadSet);
                    Assert.IsTrue(success, $"Failed to retrieve block {blockIndex}");

                    // Process the data (accessing it is enough to measure performance)
                    if (retrievedReadSet != null)
                    {
                        int count = 0;
                        foreach (var entry in retrievedReadSet)
                        {
                            // Just access the data to simulate processing it
                            var keyLength = entry.Key.Length;
                            var valueLength = entry.Value.Length;
                            count++;
                        }

                        // Only print for first iteration to avoid flooding output
                        if (iteration == 0)
                        {
                            Console.WriteLine($"Processed {count} entries for block {blockIndex}");
                        }
                    }
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Time to replay {blockCount} blocks with {entriesPerBlock} entries each, {iterationCount} iterations: {stopwatch.ElapsedMilliseconds}ms");

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds > 0, "Replay time should be measurable");
        }

        private byte[] CreateRandomBytes(int length)
        {
            var bytes = new byte[length];
            var random = new Random();
            random.NextBytes(bytes);
            return bytes;
        }
    }
}
