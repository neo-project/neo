// Copyright (C) 2015-2025 The Neo Project.
//
// BlockReadSetStorageTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Neo.Plugins.LedgerDebugger.Tests
{
    /// <summary>
    /// Tests for the BlockReadSetStorage class focusing on storage and retrieval of block read sets
    /// </summary>
    [TestClass]
    public class BlockReadSetStorageTests
    {
        private MemoryStore _memoryStore;
        private MockBlockReadSetStorage _storage;

        [TestInitialize]
        public void Setup()
        {
            _memoryStore = new MemoryStore();
            _storage = new MockBlockReadSetStorage(_memoryStore);
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up resources
            _storage.Dispose();
        }

        [TestMethod]
        public void TestAddAndTryGet_SingleBlock()
        {
            // Arrange
            uint blockIndex = 123;
            var readSet = CreateTestReadSet(5);

            // Act
            bool addSuccess = _storage.Add(blockIndex, readSet);
            bool getSuccess = _storage.TryGet(blockIndex, out var retrievedReadSet);

            // Assert
            Assert.IsTrue(addSuccess, "Add should succeed");
            Assert.IsTrue(getSuccess, "TryGet should succeed");
            Assert.IsNotNull(retrievedReadSet, "Retrieved read set should not be null");
            Assert.AreEqual(readSet.Count, retrievedReadSet.Count, "Read set count should match");

            // Verify the content matches
            readSet.AssertEquivalentTo(retrievedReadSet, "Retrieved read set should match original");
        }

        [TestMethod]
        public void TestNonExistentBlock()
        {
            // Arrange
            uint existingBlockIndex = 200;
            uint nonExistentBlockIndex = 999;

            // Act
            bool addSuccess = _storage.Add(existingBlockIndex, CreateTestReadSet(3));

            // Assert
            bool successForExisting = _storage.TryGet(existingBlockIndex, out _);
            bool successForNonExistent = _storage.TryGet(nonExistentBlockIndex, out var retrievedReadSet);

            Assert.IsTrue(addSuccess, "Add should succeed");
            Assert.IsTrue(successForExisting, "Should find existing block read set");
            Assert.IsFalse(successForNonExistent, "Should not find non-existent block read set");
            Assert.IsNull(retrievedReadSet, "Retrieved read set should be null for non-existent block");
        }

        [TestMethod]
        public void TestContentAddressableStorage_SmallValues()
        {
            // Arrange - Small values should be stored directly
            uint blockIndex = 300;
            var readSet = new Dictionary<StorageKey, StorageItem>();

            // Add 5 small values (< 32 bytes)
            for (int i = 0; i < 5; i++)
            {
                var key = CreateStorageKey(i, $"small_key_{i}");
                var value = CreateStorageItem(CreateRandomBytes(16)); // 16 bytes is less than the 32-byte threshold
                readSet[key] = value;
            }

            // Act
            bool addSuccess = _storage.Add(blockIndex, readSet);
            bool getSuccess = _storage.TryGet(blockIndex, out var retrievedReadSet);

            // Assert
            Assert.IsTrue(addSuccess, "Add should succeed");
            Assert.IsTrue(getSuccess, "TryGet should succeed for small values");
            Assert.AreEqual(readSet.Count, retrievedReadSet.Count, "Read set size should match");
        }

        [TestMethod]
        public void TestContentAddressableStorage_LargeValues()
        {
            // Arrange - Large values should be stored by hash reference
            uint blockIndex = 400;
            var readSet = new Dictionary<StorageKey, StorageItem>();

            // Add 5 large values (> 32 bytes)
            for (int i = 0; i < 5; i++)
            {
                var key = CreateStorageKey(i, $"large_key_{i}");
                var value = CreateStorageItem(CreateRandomBytes(64)); // 64 bytes exceeds the 32-byte threshold
                readSet[key] = value;
            }

            // Act
            _storage.Add(blockIndex, readSet);
            bool success = _storage.TryGet(blockIndex, out var retrievedReadSet);

            // Assert
            Assert.IsTrue(success, "Should retrieve large value read set");
            Assert.AreEqual(readSet.Count, retrievedReadSet.Count, "Read set size should match");

            foreach (var entry in readSet)
            {
                var keyBytes = entry.Key.ToArray();
                var valueBytes = entry.Value.GetValueBytes();

                bool found = false;
                foreach (var retrievedKey in retrievedReadSet.Keys)
                {
                    if (CompareByteArrays(keyBytes, retrievedKey))
                    {
                        found = true;
                        CollectionAssert.AreEqual(valueBytes, retrievedReadSet[retrievedKey], "Large value should match");
                        break;
                    }
                }

                Assert.IsTrue(found, "Key should exist in retrieved read set");
            }
        }

        [TestMethod]
        public void TestContentAddressableStorage_DuplicateValues()
        {
            // Arrange - Test value deduplication with content-addressable storage
            uint blockIndex1 = 500;
            uint blockIndex2 = 501;

            // Create shared values to use in both blocks
            var sharedValue = CreateRandomBytes(100);

            // Create two read sets with some shared values
            var readSet1 = new Dictionary<StorageKey, StorageItem>();
            var readSet2 = new Dictionary<StorageKey, StorageItem>();

            for (int i = 0; i < 5; i++)
            {
                // Block 1 entries
                var key1 = CreateStorageKey(i, $"block1_key_{i}");
                var value1 = CreateStorageItem(sharedValue); // Use shared value
                readSet1[key1] = value1;

                // Block 2 entries
                var key2 = CreateStorageKey(i, $"block2_key_{i}");
                var value2 = CreateStorageItem(sharedValue); // Use same shared value
                readSet2[key2] = value2;
            }

            // Act
            _storage.Add(blockIndex1, readSet1);
            _storage.Add(blockIndex2, readSet2);

            bool success1 = _storage.TryGet(blockIndex1, out var retrievedReadSet1);
            bool success2 = _storage.TryGet(blockIndex2, out var retrievedReadSet2);

            // Assert
            Assert.IsTrue(success1 && success2, "Should retrieve both read sets");
            Assert.AreEqual(readSet1.Count, retrievedReadSet1.Count, "Read set 1 size should match");
            Assert.AreEqual(readSet2.Count, retrievedReadSet2.Count, "Read set 2 size should match");

            // Verify values in first block
            foreach (var entry in readSet1)
            {
                var keyBytes = entry.Key.ToArray();
                var valueBytes = entry.Value.GetValueBytes();

                bool found = false;
                foreach (var retrievedKey in retrievedReadSet1.Keys)
                {
                    if (CompareByteArrays(keyBytes, retrievedKey))
                    {
                        found = true;
                        CollectionAssert.AreEqual(valueBytes, retrievedReadSet1[retrievedKey], "Shared value should match in block 1");
                        break;
                    }
                }

                Assert.IsTrue(found, "Key should exist in retrieved read set 1");
            }

            // Verify values in second block
            foreach (var entry in readSet2)
            {
                var keyBytes = entry.Key.ToArray();
                var valueBytes = entry.Value.GetValueBytes();

                bool found = false;
                foreach (var retrievedKey in retrievedReadSet2.Keys)
                {
                    if (CompareByteArrays(keyBytes, retrievedKey))
                    {
                        found = true;
                        CollectionAssert.AreEqual(valueBytes, retrievedReadSet2[retrievedKey], "Shared value should match in block 2");
                        break;
                    }
                }

                Assert.IsTrue(found, "Key should exist in retrieved read set 2");
            }
        }

        [TestMethod]
        public void TestPerformance_LargeReadSet()
        {
            // Arrange - Create a large read set with a mix of small and large values
            uint blockIndex = 600;
            int entryCount = 1000; // Test with 1000 entries
            var readSet = new Dictionary<StorageKey, StorageItem>();

            // Create a mix of small and large values
            for (int i = 0; i < entryCount; i++)
            {
                var key = CreateStorageKey(i, $"perf_key_{i}");

                // Alternate between small and large values
                var valueSize = i % 2 == 0 ? 16 : 100;
                var value = CreateStorageItem(CreateRandomBytes(valueSize));
                readSet[key] = value;
            }

            // Act - Measure add performance
            DateTime addStart = DateTime.Now;
            _storage.Add(blockIndex, readSet);
            TimeSpan addTime = DateTime.Now - addStart;

            // Act - Measure retrieval performance
            DateTime retrieveStart = DateTime.Now;
            bool success = _storage.TryGet(blockIndex, out var retrievedReadSet);
            TimeSpan retrieveTime = DateTime.Now - retrieveStart;

            // Assert
            Assert.IsTrue(success, "Should successfully retrieve large read set");
            Assert.AreEqual(entryCount, retrievedReadSet.Count, "Retrieved read set should have correct count");

            Console.WriteLine($"Add time for {entryCount} entries: {addTime.TotalMilliseconds}ms");
            Console.WriteLine($"Retrieve time for {entryCount} entries: {retrieveTime.TotalMilliseconds}ms");
        }

        #region Helper Methods

        private Dictionary<StorageKey, StorageItem> CreateTestReadSet(int count)
        {
            var readSet = new Dictionary<StorageKey, StorageItem>();

            for (int i = 0; i < count; i++)
            {
                var key = CreateStorageKey(i, $"test_key_{i}");
                var value = CreateStorageItem(CreateRandomBytes(20)); // 20 bytes value
                readSet[key] = value;
            }

            return readSet;
        }

        private StorageKey CreateStorageKey(int id, string keyString)
        {
            return new StorageKey
            {
                Id = (byte)id,
                Key = System.Text.Encoding.UTF8.GetBytes(keyString)
            };
        }

        private StorageItem CreateStorageItem(byte[] data)
        {
            return new StorageItem { Value = data };
        }

        private byte[] CreateRandomBytes(int size)
        {
            var data = new byte[size];
            Random.Shared.NextBytes(data);
            return data;
        }

        private bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            return a.SequenceEqual(b);
        }

        #endregion
    }
}
