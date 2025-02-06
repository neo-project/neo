// Copyright (C) 2015-2025 The Neo Project.
//
// UT_SerializedCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
using System;

namespace Neo.UnitTests.Persistence
{
    // Dummy implementations for IStorageCacheEntry for testing purposes
    public class TestCacheEntry(int value) : IStorageCacheEntry
    {
        public int Value { get; } = value;
        public StorageItem GetStorageItem() => new() { Value = BitConverter.GetBytes(Value) };
    }

    public class TestCacheEntry2(string text) : IStorageCacheEntry
    {
        public string Text { get; } = text;
        public StorageItem GetStorageItem() => new() { Value = System.Text.Encoding.UTF8.GetBytes(Text) };
    }

    [TestClass]
    [TestCategory("SerializedCache")]
    public class UT_SerializedCache
    {
        [TestMethod]
        public void TestGetReturnsDefaultWhenNotSet()
        {
            var cache = new SerializedCache();
            Assert.IsNull(cache.Get<TestCacheEntry>(), "Expected null when cache does not contain the type");
        }

        [TestMethod]
        public void TestSetAndGet()
        {
            var entry = new TestCacheEntry(42);
            var cache = new SerializedCache();
            cache.Set<TestCacheEntry>(entry);
            var retrieved = cache.Get<TestCacheEntry>();
            Assert.IsNotNull(retrieved, "Entry was not set correctly");
            Assert.AreEqual(42, retrieved.Value, "Retrieved entry does not match the set value");
        }

        [TestMethod]
        public void TestRemove()
        {
            var entry = new TestCacheEntry(42);
            var cache = new SerializedCache();
            cache.Set<TestCacheEntry>(entry);
            cache.Remove(typeof(TestCacheEntry));
            Assert.IsNull(cache.Get<TestCacheEntry>(), "Entry should be null after removal");
        }

        [TestMethod]
        public void TestClear()
        {
            var cache = new SerializedCache();
            cache.Set<TestCacheEntry>(new TestCacheEntry(1));
            cache.Set<TestCacheEntry2>(new TestCacheEntry2("one"));
            cache.Clear();
            Assert.IsNull(cache.Get<TestCacheEntry>(), "Cache should be cleared for first type");
            Assert.IsNull(cache.Get<TestCacheEntry2>(), "Cache should be cleared for second type");
        }

        [TestMethod]
        public void TestCopyFrom()
        {
            var source = new SerializedCache();
            source.Set<TestCacheEntry>(new TestCacheEntry(99));

            var target = new SerializedCache();
            target.Set<TestCacheEntry2>(new TestCacheEntry2("hello"));

            target.CopyFrom(source);

            var entryA = target.Get<TestCacheEntry>();
            var entryB = target.Get<TestCacheEntry2>();

            Assert.IsNotNull(entryA, "Copied entry should exist in target cache");
            Assert.AreEqual(99, entryA.Value, "Copied entry value mismatch");
            Assert.IsNotNull(entryB, "Existing entry in target cache should not be overwritten if types differ");
            Assert.AreEqual("hello", entryB.Text, "Existing entry text mismatch");
        }

        [TestMethod]
        public void TestSetNullValueRemovesEntry()
        {
            var cache = new SerializedCache();
            var entry = new TestCacheEntry(42);
            cache.Set<TestCacheEntry>(entry);
            Assert.IsNotNull(cache.Get<TestCacheEntry>(), "Entry should be set initially");

            cache.Set<TestCacheEntry>(null);
            Assert.IsNull(cache.Get<TestCacheEntry>(), "Entry should be removed after setting null");
        }

        [TestMethod]
        public void TestMultipleTypesStorage()
        {
            var cache = new SerializedCache();
            var entry1 = new TestCacheEntry(42);
            var entry2 = new TestCacheEntry2("test");

            cache.Set<TestCacheEntry>(entry1);
            cache.Set<TestCacheEntry2>(entry2);

            var retrieved1 = cache.Get<TestCacheEntry>();
            var retrieved2 = cache.Get<TestCacheEntry2>();

            Assert.IsNotNull(retrieved1, "First entry should be retrievable");
            Assert.IsNotNull(retrieved2, "Second entry should be retrievable");
            Assert.AreEqual(42, retrieved1.Value, "First entry value mismatch");
            Assert.AreEqual("test", retrieved2.Text, "Second entry text mismatch");
        }

        [TestMethod]
        public void TestOverwriteExistingEntry()
        {
            var cache = new SerializedCache();
            cache.Set<TestCacheEntry>(new TestCacheEntry(42));
            cache.Set<TestCacheEntry>(new TestCacheEntry(99));

            var retrieved = cache.Get<TestCacheEntry>();
            Assert.IsNotNull(retrieved, "Entry should exist");
            Assert.AreEqual(99, retrieved.Value, "Entry should be overwritten with new value");
        }

        [TestMethod]
        public void TestCopyFromEmptyCache()
        {
            var source = new SerializedCache();
            var target = new SerializedCache();
            target.Set<TestCacheEntry>(new TestCacheEntry(42));

            target.CopyFrom(source);

            var entry = target.Get<TestCacheEntry>();
            Assert.IsNotNull(entry, "Existing entry should remain after copying from empty cache");
            Assert.AreEqual(42, entry.Value, "Existing entry value should be unchanged");
        }

        [TestMethod]
        public void TestThreadSafety()
        {
            var cache = new SerializedCache();
            var tasks = new System.Threading.Tasks.Task[10];

            for (int i = 0; i < tasks.Length; i++)
            {
                var value = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    cache.Set<TestCacheEntry>(new TestCacheEntry(value));
                    cache.Get<TestCacheEntry>();
                    if (value % 2 == 0)
                        cache.Remove(typeof(TestCacheEntry));
                });
            }

            System.Threading.Tasks.Task.WaitAll(tasks);
            // If we get here without exceptions, the test passes
            // The final state is non-deterministic due to race conditions,
            // but the operations should be thread-safe
        }

        [TestMethod]
        public void TestConcurrentCopyFrom()
        {
            var source = new SerializedCache();
            var target = new SerializedCache();
            source.Set<TestCacheEntry>(new TestCacheEntry(42));

            var tasks = new System.Threading.Tasks.Task[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    target.CopyFrom(source);
                });
            }

            System.Threading.Tasks.Task.WaitAll(tasks);
            var entry = target.Get<TestCacheEntry>();
            Assert.IsNotNull(entry, "Entry should be copied successfully under concurrent operations");
            Assert.AreEqual(42, entry.Value, "Copied entry should have correct value");
        }
    }
}
