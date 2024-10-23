// Copyright (C) 2015-2024 The Neo Project.
//
// StoreSnapshotTest.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Data.LevelDB;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Plugins.Storage.Tests;

/*
 * LevelDB Thread Safety Explanation:
 *
 * LevelDB is designed to be a fast key-value storage library. However, it has
 * some limitations regarding thread safety. Specifically, LevelDB is not thread-safe
 * when multiple threads are attempting to write to the database concurrently. This can
 * lead to data corruption, crashes, and other undefined behaviors.
 *
 * LevelDB provides snapshots and batch writes. Snapshots allow
 * a consistent view of the database at a point in time, but they are not designed for
 * concurrent write operations. Batch writes can be used to perform atomic updates,
 * but they also need to be managed carefully to avoid concurrency issues.
 *
 * In this test class, we demonstrate these thread safety issues and how to mitigate
 * them using different approaches such as locking mechanisms and creating separate
 * snapshots for each thread.
 */
partial class StoreTest
{
    [TestMethod]
    [ExpectedException(typeof(AggregateException))]
    public void TestMultiThreadLevelDbSnapshotPut()
    {
        using var store = levelDbStore.GetStore(path_leveldb);
        using var snapshot = store.GetSnapshot();
        var testKey = new byte[] { 0x01, 0x02, 0x03 };

        var threadCount = 1;
        while (true)
        {
            var tasks = new Task[threadCount];
            try
            {
                for (var i = 0; i < tasks.Length; i++)
                {
                    var value = new byte[] { 0x04, 0x05, 0x06, (byte)i };
                    tasks[i] = Task.Run(() =>
                    {
                        // Introduce delay to increase conflict chance
                        Thread.Sleep(Random.Shared.Next(1, 10));
                        // Attempt to write to the snapshot and commit
                        snapshot.Put(testKey, value);
                        snapshot.Commit();
                    });
                }

                // Wait for all tasks to complete
                Task.WaitAll(tasks);
                threadCount++;
            }
            catch (LevelDBException ex)
            {
                // LevelDBException is also possible due to LevelDB being thread-unsafe
                Console.WriteLine($"LevelDBException caught with {threadCount} threads: {ex.Message}");
                break;
            }
            catch (Exception ex)
            {
                // It could be aggregated exception where LevelDBException is just one of them
                Console.WriteLine("Unexpected exception: " + ex.Message);
                throw;
            }
        }
    }

    [TestMethod]
    public void TestMultiThreadLevelDbSnapshotPutUntilException()
    {
        using var store = levelDbStore.GetStore(path_leveldb);
        using var snapshot = store.GetSnapshot();
        var testKey = new byte[] { 0x01, 0x02, 0x03 };

        var threadCount = 1;
        while (true)
        {
            var tasks = new Task[threadCount];
            try
            {
                for (var i = 0; i < tasks.Length; i++)
                {
                    var value = new byte[] { 0x04, 0x05, 0x06, (byte)i };
                    tasks[i] = Task.Run(() =>
                    {
                        // Introduce delay to increase conflict chance
                        Thread.Sleep(Random.Shared.Next(1, 100));
                        // Attempt to write to the snapshot without committing
                        snapshot.Put(testKey, value);
                    });
                }

                // Wait for all tasks to complete
                Task.WaitAll(tasks);

                // Attempt to commit the changes
                snapshot.Commit();
                threadCount++;
            }
            catch (LevelDBException ex)
            {
                // LevelDBException is also possible due to LevelDB being thread-unsafe
                Console.WriteLine($"LevelDBException caught with {threadCount} threads.");
                break;
            }
            catch (Exception ex)
            {
                // It could be aggregated exception where LevelDBException is just one of them
                Console.WriteLine("Unexpected exception: " + ex.Message);
                break;
            }
        }
    }

    [TestMethod]
    public void TestMultiThreadLevelDbSnapshotPutWithLocker()
    {
        using var store = levelDbStore.GetStore(path_leveldb);

        object locker = new();
        var testKey = new byte[] { 0x01, 0x02, 0x03 };

        var tasks = new Task[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            var value = new byte[] { 0x04, 0x05, 0x06, (byte)i };
            tasks[i] = Task.Run(() =>
            {
                using var snapshot = store.GetSnapshot();
                // Use a lock to ensure thread-safe access to the snapshot
                lock (locker)
                {
                    snapshot.Put(testKey, value);
                    snapshot.Commit();
                }
            });
        }

        // Wait for all tasks to complete
        Task.WaitAll(tasks);
    }

    [TestMethod]
    public void TestOneSnapshotPerThreadLevelDbSnapshotPut()
    {
        using var store = levelDbStore.GetStore(path_leveldb);
        var testKey = new byte[] { 0x01, 0x02, 0x03 };

        var tasks = new Task[1000];
        for (var i = 0; i < tasks.Length; i++)
        {
            var value = new byte[] { 0x04, 0x05, 0x06, (byte)i };
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    // Create a new snapshot for each thread to avoid concurrent access issues
                    using var snapshot = store.GetSnapshot();
                    snapshot.Put(testKey, value);
                    snapshot.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Task {i} encountered an exception: {ex}");
                    throw;
                }
            });
        }

        // Wait for all tasks to complete
        Task.WaitAll(tasks);
    }
}
