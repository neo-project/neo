// Copyright (C) 2015-2025 The Neo Project.
//
// TestExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.LedgerDebugger.Tests
{
    /// <summary>
    /// Extension methods to help with testing the LedgerDebugger plugin.
    /// Provides conversion utilities and assertion helpers for working with Neo types.
    /// </summary>
    public static class TestExtensions
    {
        /// <summary>
        /// Converts a ReadOnlySpan to byte array.
        /// </summary>
        /// <param name="span">The span to convert.</param>
        /// <returns>A new byte array containing a copy of the span data.</returns>
        public static byte[] AsArray(this ReadOnlySpan<byte> span)
        {
            var array = new byte[span.Length];
            span.CopyTo(array);
            return array;
        }

        /// <summary>
        /// Converts a ReadOnlyMemory to byte array.
        /// </summary>
        /// <param name="memory">The memory to convert.</param>
        /// <returns>A new byte array containing a copy of the memory data.</returns>
        public static byte[] AsArray(this ReadOnlyMemory<byte> memory)
        {
            var array = new byte[memory.Length];
            memory.Span.CopyTo(array);
            return array;
        }

        /// <summary>
        /// Converts the Value property of a StorageItem to a byte array.
        /// </summary>
        /// <param name="item">The storage item whose value to convert.</param>
        /// <returns>A byte array representation of the storage item's value.</returns>
        public static byte[] GetValueBytes(this StorageItem item)
        {
            return item.Value.Span.ToArray();
        }

        /// <summary>
        /// Compares two dictionaries of storage keys and items for equality.
        /// </summary>
        /// <param name="readSet">The first dictionary to compare.</param>
        /// <param name="otherReadSet">The second dictionary to compare.</param>
        /// <returns>True if the dictionaries contain the same keys and values, false otherwise.</returns>
        public static bool IsEquivalentTo(this Dictionary<StorageKey, StorageItem> readSet,
                                          Dictionary<StorageKey, StorageItem> otherReadSet)
        {
            if (readSet.Count != otherReadSet.Count)
                return false;

            foreach (var entry in readSet)
            {
                var keyBytes = entry.Key.ToArray();
                var valueBytes = entry.Value.GetValueBytes();

                bool found = false;
                foreach (var otherKey in otherReadSet.Keys)
                {
                    if (keyBytes.SequenceEqual(otherKey.ToArray()))
                    {
                        found = true;
                        if (!valueBytes.SequenceEqual(otherReadSet[otherKey].GetValueBytes()))
                            return false;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Asserts that two storage dictionaries are equivalent by comparing keys and values.
        /// </summary>
        /// <param name="readSet">The expected dictionary.</param>
        /// <param name="otherReadSet">The actual dictionary to verify.</param>
        /// <param name="message">Optional failure message.</param>
        public static void AssertEquivalentTo(this Dictionary<StorageKey, StorageItem> readSet,
                                             Dictionary<StorageKey, StorageItem> otherReadSet,
                                             string message = "Read sets should be equivalent")
        {
            Assert.IsTrue(readSet.IsEquivalentTo(otherReadSet), message);
        }

        /// <summary>
        /// Compares a dictionary of StorageKey/StorageItem with a dictionary of byte[]/byte[] for equality.
        /// Used when comparing original read sets with those retrieved from BlockReadSetStorage.
        /// </summary>
        /// <param name="originalReadSet">The original dictionary with StorageKey and StorageItem.</param>
        /// <param name="retrievedReadSet">The retrieved dictionary with byte[] keys and values.</param>
        /// <returns>True if the dictionaries contain equivalent keys and values, false otherwise.</returns>
        public static bool IsEquivalentTo(this Dictionary<StorageKey, StorageItem> originalReadSet,
                                         Dictionary<byte[], byte[]> retrievedReadSet)
        {
            if (originalReadSet.Count != retrievedReadSet.Count)
                return false;

            foreach (var entry in originalReadSet)
            {
                var keyBytes = entry.Key.ToArray();
                var valueBytes = entry.Value.GetValueBytes();

                bool found = false;
                foreach (var retrievedKey in retrievedReadSet.Keys)
                {
                    // Use SequenceEqual for byte array comparison
                    if (keyBytes.SequenceEqual(retrievedKey))
                    {
                        found = true;
                        // Also compare the values using SequenceEqual
                        if (!valueBytes.SequenceEqual(retrievedReadSet[retrievedKey]))
                            return false;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Asserts that a StorageKey/StorageItem dictionary is equivalent to a byte[]/byte[] dictionary.
        /// </summary>
        /// <param name="originalReadSet">The original dictionary with StorageKey and StorageItem.</param>
        /// <param name="retrievedReadSet">The retrieved dictionary with byte[] keys and values.</param>
        /// <param name="message">Optional failure message.</param>
        public static void AssertEquivalentTo(this Dictionary<StorageKey, StorageItem> originalReadSet,
                                             Dictionary<byte[], byte[]> retrievedReadSet,
                                             string message = "Read sets should be equivalent")
        {
            Assert.IsTrue(originalReadSet.IsEquivalentTo(retrievedReadSet), message);
        }
    }
}
