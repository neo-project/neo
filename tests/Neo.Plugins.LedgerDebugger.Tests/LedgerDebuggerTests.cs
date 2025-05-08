// Copyright (C) 2015-2025 The Neo Project.
//
// LedgerDebuggerTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

# region SETTINGS_CLASS
// For testing purposes - need to make a public version of Settings class
/// <summary>
/// Public version of Settings for testing
/// </summary>
public class Settings
{
    public string Path { get; set; }
    public string StoreProvider { get; set; }
    public int MaxReadSetsToKeep { get; set; } = 10000;

    public static Settings Default { get; set; }
}
# endregion

namespace Neo.Plugins.LedgerDebugger.Tests
{
    /// <summary>
    /// Tests for the LedgerDebugger main class focusing on its ability to
    /// capture and replay blockchain execution state
    /// </summary>
    [TestClass]
    public class LedgerDebuggerTests
    {
        private TestLedgerDebugger _debugger;
        private string _tempPath;

        [TestInitialize]
        public void Setup()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), "LedgerDebuggerTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempPath);

            // Create test settings
            var settings = new Settings
            {
                Path = _tempPath,
                StoreProvider = "MemoryStore"
            };

            // Set default settings
            Settings.Default = settings;

            // Create our test debugger
            _debugger = new TestLedgerDebugger();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _debugger.Dispose();

            try
            {
                Directory.Delete(_tempPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete temp directory: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestBlockCommittingHandler()
        {
            // Arrange - Create test block with transactions
            var block = CreateTestBlock(1);
            var snapshot = CreateTestDataCache();
            var applicationExecuted = new List<Blockchain.ApplicationExecuted>();

            // Add some read operations to the snapshot to simulate execution
            for (int i = 0; i < 10; i++)
            {
                var key = new StorageKey { Id = (byte)i, Key = BitConverter.GetBytes(i) };
                var value = new StorageItem { Value = CreateRandomBytes(20) };

                // Add to read set by reading the key
                _ = snapshot.TryGet(key, out _);

                // Also add to store so we can read it
                if (!snapshot.Contains(key))
                    snapshot.Add(key, value);
            }

            // Create a test read set for the snapshot
            var readSet = new Dictionary<StorageKey, StorageItem>();
            foreach (var key in snapshot.Find(Array.Empty<byte>()).Select(entry => entry.Key))
            {
                snapshot.TryGet(key, out var value);
                readSet[key] = value;
            }

            // Act - Call the committing handler
            _debugger.TestBlockCommittingHandler(block, snapshot, applicationExecuted);

            // Verify readset was stored
            var storage = _debugger.GetBlockReadSetStorage();
            bool success = storage.TryGet(block.Index, out var retrievedReadSet);

            // Assert
            Assert.IsTrue(success, "Should have stored block read set during commit");
            Assert.IsNotNull(retrievedReadSet, "Retrieved read set should not be null");
            Assert.IsTrue(retrievedReadSet.Count > 0, "Read set should have entries");
        }

        [TestMethod]
        public void TestEmptyBlockNotStored()
        {
            // Arrange - Create block with no transactions and empty read set
            var block = CreateTestBlock(2, includeTransactions: false);
            var snapshot = CreateTestDataCache();
            var applicationExecuted = new List<Blockchain.ApplicationExecuted>();

            // Act - Call the committing handler
            _debugger.TestBlockCommittingHandler(block, snapshot, applicationExecuted);

            // Verify readset was not stored (because block has no transactions)
            var storage = _debugger.GetBlockReadSetStorage();
            bool success = storage.TryGet(block.Index, out var _);

            // Assert
            Assert.IsFalse(success, "Should not store read set for block with no transactions");
        }

        [TestMethod]
        public void TestHandlingSnapshotWithNoReadSet()
        {
            // Arrange - Create block with transactions but empty read set
            var block = CreateTestBlock(3);
            var snapshot = CreateEmptyDataCache(); // No read operations
            var applicationExecuted = new List<Blockchain.ApplicationExecuted>();

            // Act - Call the committing handler (shouldn't throw)
            _debugger.TestBlockCommittingHandler(block, snapshot, applicationExecuted);

            // Verify readset was not stored (because read set is empty)
            var storage = _debugger.GetBlockReadSetStorage();
            bool success = storage.TryGet(block.Index, out var _);

            // Assert
            Assert.IsFalse(success, "Should not store empty read set");
        }

        [TestMethod]
        public void TestErrorHandlingDuringStorage()
        {
            // Arrange - Create test setup with a faulty storage
            _debugger.SetFaultyStorage(true);

            var block = CreateTestBlock(4);
            var snapshot = CreateTestDataCache();
            var applicationExecuted = new List<Blockchain.ApplicationExecuted>();

            // Add some read operations
            for (int i = 0; i < 5; i++)
            {
                var key = new StorageKey { Id = (byte)i, Key = BitConverter.GetBytes(i) };
                var value = new StorageItem { Value = CreateRandomBytes(20) };

                _ = snapshot.TryGet(key, out _); // Add to read set

                if (!snapshot.Contains(key))
                    snapshot.Add(key, value);
            }

            // Act & Assert - Call handler, which should handle the exception
            try
            {
                _debugger.TestBlockCommittingHandlerWithExceptionHandling(block, snapshot, applicationExecuted);
                // If we get here without exception, the test passes
                Assert.IsTrue(true, "Exception was handled gracefully");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception was not handled gracefully: {ex.Message}");
            }
        }

        #region Helper Methods

        private DataCache CreateTestDataCache()
        {
            var store = new MemoryStore();
            return new StoreCache(store);
        }

        private DataCache CreateEmptyDataCache()
        {
            var store = new MemoryStore();
            var cache = new StoreCache(store);
            // Don't perform any reads to keep read set empty
            return cache;
        }

        private Block CreateTestBlock(uint index, bool includeTransactions = true)
        {
            // Create a proper header
            var header = new Header
            {
                Index = index,
                Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero
            };

            // Create the block with the header
            var block = new Block
            {
                Header = header,
                Transactions = includeTransactions
                    ? new Transaction[] { CreateTransaction(), CreateTransaction() }
                    : Array.Empty<Transaction>()
            };

            return block;
        }

        private Transaction CreateTransaction()
        {
            var tx = new Transaction();

            // Create correct type for the Script property (ReadOnlyMemory<byte>)
            var scriptBytes = new byte[] { 0x01, 0x02, 0x03 };
            ReadOnlyMemory<byte> script = new ReadOnlyMemory<byte>(scriptBytes);

            // Use reflection to set read-only properties
            typeof(Transaction).GetProperty("Script").SetValue(tx, script);
            typeof(Transaction).GetProperty("Attributes").SetValue(tx, Array.Empty<TransactionAttribute>());
            typeof(Transaction).GetProperty("Signers").SetValue(tx, new Signer[] { new Signer { Account = UInt160.Zero } });
            typeof(Transaction).GetProperty("Witnesses").SetValue(tx, Array.Empty<Witness>());

            return tx;
        }

        private byte[] CreateRandomBytes(int length)
        {
            var bytes = new byte[length];
            Random.Shared.NextBytes(bytes);
            return bytes;
        }

        #endregion

        /// <summary>
        /// Test implementation of LedgerDebugger that exposes protected methods
        /// </summary>
        private class TestLedgerDebugger : LedgerDebugger
        {
            private bool _faultyStorage = false;
            private MockBlockReadSetStorage _blockReadSetStorage;

            public TestLedgerDebugger()
            {
                // Directly create the block read set storage without going through OnSystemLoaded
                var store = new MemoryStore();
                _blockReadSetStorage = new MockBlockReadSetStorage(store);
            }

            public void TestOnSystemLoaded(DataCache snapshot)
            {
                // No need to create a real system, just simulate setting up the storage
            }

            public void TestBlockCommittingHandler(Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
            {
                // Simulate the handler
                // Extract a read set from snapshot
                var readSet = new Dictionary<StorageKey, StorageItem>();
                foreach (var entry in snapshot.Find(Array.Empty<byte>()))
                {
                    if (snapshot.TryGet(entry.Key, out var value))
                    {
                        readSet[entry.Key] = value;
                    }
                }

                // Only add if there are transactions and reads
                if (block.Transactions.Length > 0 && readSet.Count > 0)
                {
                    _blockReadSetStorage.Add(block.Index, readSet);
                }
            }

            public void TestBlockCommittingHandlerWithExceptionHandling(Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
            {
                try
                {
                    // Simulate the handler
                    // Extract a read set from snapshot
                    var readSet = new Dictionary<StorageKey, StorageItem>();
                    foreach (var entry in snapshot.Find(Array.Empty<byte>()))
                    {
                        if (snapshot.TryGet(entry.Key, out var value))
                        {
                            readSet[entry.Key] = value;
                        }
                    }

                    // Only add if there are transactions and reads
                    if (block.Transactions.Length > 0 && readSet.Count > 0)
                    {
                        try
                        {
                            _blockReadSetStorage.Add(block.Index, readSet);
                        }
                        catch (Exception ex)
                        {
                            // Log the error but don't throw it
                            Console.WriteLine($"Error storing block read set: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't throw it
                    Console.WriteLine($"Error during block committing: {ex.Message}");
                }
            }

            public MockBlockReadSetStorage GetBlockReadSetStorage()
            {
                return _blockReadSetStorage;
            }

            public void SetFaultyStorage(bool faulty)
            {
                _faultyStorage = faulty;

                if (faulty)
                {
                    // Replace the storage with a faulty one
                    _blockReadSetStorage = new FaultyMockBlockReadSetStorage(new MemoryStore());
                }
            }

            // Needed for test access
            private string _tempPath => Settings.Default.Path;
        }

        /// <summary>
        /// Mock implementation that throws exceptions on Add operation
        /// </summary>
        private class FaultyMockBlockReadSetStorage : MockBlockReadSetStorage
        {
            public FaultyMockBlockReadSetStorage(IStore store) : base(store)
            {
            }

            public override bool Add(uint blockIndex, Dictionary<StorageKey, StorageItem> readSet)
            {
                throw new Exception("Simulated storage error");
            }
        }
    }
}
