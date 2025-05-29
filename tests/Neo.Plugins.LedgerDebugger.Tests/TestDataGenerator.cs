// Copyright (C) 2015-2025 The Neo Project.
//
// TestDataGenerator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Neo.Plugins.LedgerDebugger.Tests
{
    /// <summary>
    /// Utility class for generating test data for LedgerDebugger tests.
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Random random = new Random();

        /// <summary>
        /// Generates a byte array of specified size filled with random data.
        /// </summary>
        /// <param name="size">The size of the array to generate.</param>
        /// <returns>A byte array filled with random data.</returns>
        public static byte[] GenerateRandomBytes(int size)
        {
            byte[] data = new byte[size];
            random.NextBytes(data);
            return data;
        }

        /// <summary>
        /// Generates a sample storage key for testing.
        /// </summary>
        /// <param name="id">Optional identifier to make the key unique.</param>
        /// <returns>A storage key for testing.</returns>
        public static StorageKey GenerateStorageKey(int id = 0)
        {
            byte[] keyBytes = new byte[20]; // Typical contract hash size
            random.NextBytes(keyBytes);

            // Make it unique if an ID is provided
            if (id > 0)
            {
                BitConverter.GetBytes(id).CopyTo(keyBytes, 0);
            }

            return new StorageKey
            {
                Id = (byte)id,
                Key = keyBytes
            };
        }

        /// <summary>
        /// Generates a storage item with a value of the specified size.
        /// </summary>
        /// <param name="valueSize">The size of the value to generate.</param>
        /// <returns>A storage item with random data of the specified size.</returns>
        public static StorageItem GenerateStorageItem(int valueSize)
        {
            return new StorageItem
            {
                Value = GenerateRandomBytes(valueSize)
            };
        }

        /// <summary>
        /// Generates a dictionary of storage keys and items of various sizes.
        /// </summary>
        /// <param name="count">The number of key-value pairs to generate.</param>
        /// <param name="smallValuePercentage">Percentage of values that should be small (≤ 32 bytes).</param>
        /// <param name="smallValueSize">The size of small values.</param>
        /// <param name="largeValueSize">The size of large values.</param>
        /// <returns>A dictionary containing the generated storage keys and items.</returns>
        public static Dictionary<StorageKey, StorageItem> GenerateReadSet(
            int count,
            double smallValuePercentage = 0.5,
            int smallValueSize = 16,
            int largeValueSize = 100)
        {
            var readSet = new Dictionary<StorageKey, StorageItem>();

            for (int i = 0; i < count; i++)
            {
                var key = GenerateStorageKey(i + 1);
                var valueSize = random.NextDouble() < smallValuePercentage ? smallValueSize : largeValueSize;
                var item = GenerateStorageItem(valueSize);

                readSet[key] = item;
            }

            return readSet;
        }

        /// <summary>
        /// Generates a dictionary with a large number of entries that have duplicate values.
        /// Useful for testing deduplication functionality.
        /// </summary>
        /// <param name="uniqueValueCount">Number of unique values to generate.</param>
        /// <param name="duplicatesPerValue">Number of times each value should be duplicated.</param>
        /// <param name="valueSize">Size of each value.</param>
        /// <returns>A dictionary with the specified number of entries and duplicated values.</returns>
        public static Dictionary<StorageKey, StorageItem> GenerateDuplicatedValueReadSet(
            int uniqueValueCount,
            int duplicatesPerValue,
            int valueSize = 100)
        {
            var readSet = new Dictionary<StorageKey, StorageItem>();
            var uniqueValues = new List<byte[]>();

            // Generate unique values
            for (int i = 0; i < uniqueValueCount; i++)
            {
                uniqueValues.Add(GenerateRandomBytes(valueSize));
            }

            // Create entries with duplicated values
            int keyCounter = 0;
            foreach (var value in uniqueValues)
            {
                for (int i = 0; i < duplicatesPerValue; i++)
                {
                    var key = GenerateStorageKey(++keyCounter);
                    var item = new StorageItem { Value = value };
                    readSet[key] = item;
                }
            }

            return readSet;
        }

        /// <summary>
        /// Creates a snapshot with the given read set for testing.
        /// </summary>
        /// <param name="readSet">The read set to include in the snapshot.</param>
        /// <returns>A memory store containing the read set.</returns>
        public static MemoryStore CreateSnapshotWithReadSet(Dictionary<StorageKey, StorageItem> readSet)
        {
            var store = new MemoryStore();
            var storeCache = new StoreCache(store);  // Use StoreCache instead of direct snapshot

            foreach (var kvp in readSet)
            {
                storeCache.Add(kvp.Key, kvp.Value);  // Use Add method instead of Put
            }

            storeCache.Commit();
            return store;
        }

        /// <summary>
        /// Generates a hash from the given data using SHA256.
        /// </summary>
        /// <param name="data">The data to hash.</param>
        /// <returns>The SHA256 hash of the data.</returns>
        public static byte[] GenerateHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(data);
        }

        /// <summary>
        /// Generates a read set with a more random distribution of key/value sizes
        /// for use in performance testing.
        /// </summary>
        /// <param name="entryCount">Number of entries to generate</param>
        /// <param name="smallValuePercentage">Percentage (0.0-1.0) of values that should be small</param>
        /// <param name="smallValueSize">Size in bytes for small values</param>
        /// <param name="largeValueSize">Size in bytes for large values</param>
        /// <returns>Dictionary of generated StorageKey/StorageItem pairs</returns>
        public static Dictionary<StorageKey, StorageItem> GenerateRandomReadSet(
            int entryCount,
            double smallValuePercentage = 0.8,
            int smallValueSize = 16,
            int largeValueSize = 128)
        {
            var readSet = new Dictionary<StorageKey, StorageItem>();
            var random = new Random();

            for (int i = 0; i < entryCount; i++)
            {
                // Create an ID between 0-255
                byte id = (byte)random.Next(0, 256);

                // Create random key with variable length between 4-32 bytes
                int keyLength = random.Next(4, 33);
                byte[] keyBytes = new byte[keyLength];
                random.NextBytes(keyBytes);

                var key = new StorageKey
                {
                    Id = id,
                    Key = keyBytes
                };

                // Determine if this should be a small or large value based on percentage
                bool isSmallValue = random.NextDouble() < smallValuePercentage;
                int valueSize = isSmallValue ? smallValueSize : largeValueSize;

                // Create random value
                byte[] valueBytes = new byte[valueSize];
                random.NextBytes(valueBytes);

                var value = new StorageItem { Value = valueBytes };

                // Add to read set, avoiding duplicates
                if (!readSet.ContainsKey(key))
                {
                    readSet[key] = value;
                }
                else
                {
                    // If key already exists, try again with a different key
                    i--;
                }
            }

            return readSet;
        }
    }
}
