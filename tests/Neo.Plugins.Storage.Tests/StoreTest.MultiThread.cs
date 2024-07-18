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
using System.Linq;
using System.Threading.Tasks;

namespace Neo.Plugins.Storage.Tests;

partial class StoreTest
{

    [TestMethod]
    [ExpectedException(typeof(AggregateException))]
    public void TestMultiThreadLevelDbSnapshotPut()
    {
        using var store = levelDbStore.GetStore(path_leveldb);
        var snapshot = store.GetSnapshot();
        var testKey = new byte[] { 0x01, 0x02, 0x03 };

        var tasks = new Task[100];
        for (var i = 0; i < tasks.Length; i++)
        {
            var value = new byte[] { 0x04, 0x05, 0x06, (byte)i };
            tasks[i] = Task.Run(() =>
            {
                snapshot.Put(testKey, value);
                snapshot.Commit();
            });
        }
        Task.WaitAll(tasks);
        snapshot.Dispose();
    }

    [TestMethod]
    public void TestMultiThreadLevelDbSnapshotPutWithoutCommit()
    {
        using var store = levelDbStore.GetStore(path_leveldb);
        var snapshot = store.GetSnapshot();
        var testKey = new byte[] { 0x01, 0x02, 0x03 };

        var tasks = new Task[100];
        for (var i = 0; i < tasks.Length; i++)
        {
            var value = new byte[] { 0x04, 0x05, 0x06, (byte)i };
            tasks[i] = Task.Run(() =>
            {
                snapshot.Put(testKey, value);
            });
        }

        try
        {
            Task.WaitAll(tasks);
            snapshot.Commit();
        }
        catch (AggregateException ae)
        {
            var innerExceptions = ae.InnerExceptions;
            var hasExpectedException = innerExceptions.Any(innerException => innerException is AggregateException or LevelDBException);

            if (!hasExpectedException)
            {
                // Re-throw if none of the expected exceptions were found
                throw;
            }
        }
        finally
        {
            snapshot.Dispose();
        }
    }


    [TestMethod]
    public void TestMultiThreadLevelDbSnapshotPutWithLocker()
    {
        using var store = levelDbStore.GetStore(path_leveldb);

        object locker = new();
        var snapshot = store.GetSnapshot();

        var testKey = new byte[] { 0x01, 0x02, 0x03 };

        var tasks = new Task[100];
        for (var i = 0; i < tasks.Length; i++)
        {
            var value = new byte[] { 0x04, 0x05, 0x06, (byte)i };
            tasks[i] = Task.Run(() =>
            {
                lock (locker)
                {
                    snapshot.Put(testKey, value);
                    snapshot.Commit();
                }
            });
        }
        Task.WaitAll(tasks);
        snapshot.Dispose();
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
                var snapshot = store.GetSnapshot();
                snapshot.Put(testKey, value);
                snapshot.Commit();
                snapshot.Dispose();
            });
        }
        Task.WaitAll(tasks);
    }
}
