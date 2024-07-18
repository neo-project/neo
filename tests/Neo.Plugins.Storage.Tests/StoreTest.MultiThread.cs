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

using Akka.Util.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Neo.Plugins.Storage.Tests;


partial class StoreTest
{

    #region Async Tests for OnCommitting and OnCommitted

    // Tests bellow are tests for the async delegate issue
    // first reported by Vitor (vncoelho) in https://github.com/neo-project/neo/issues/3356

    [TestMethod]
    public void TestOneThreadLevelDbSnapshotPut()
    {
        using var store = levelDbStore.GetStore(path_leveldb);
        var snapshot = store.GetSnapshot();

        var testKey = new byte[] { 0x01, 0x02, 0x03 };

        for (var i = 0; i < 2; i++)
        {
            var value = new byte[] { 0x04, 0x05, 0x06, (byte)i };
            snapshot.Put(testKey, value);
            snapshot.Commit();
        }
        snapshot.Dispose();
    }

    [TestMethod]
    public void TestSingleExtraThreadLevelDbSnapshotPut()
    {
        using var store = levelDbStore.GetStore(path_leveldb);

        object locker = new();
        var snapshot = store.GetSnapshot();

        var testKey = new byte[] { 0x01, 0x02, 0x03 };

        var tasks = new Task[1];
        for (var i = 0; i < tasks.Length; i++)
        {
            var value = new byte[] { 0x04, 0x05, 0x06, (byte)i };
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    lock (locker)
                    {
                        snapshot.Put(testKey, value);
                        snapshot.Commit();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in task: {ex.Message}");
                    throw;
                }
            });
        }

        try
        {
            Task.WaitAll(tasks);
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.InnerExceptions)
            {
                Console.WriteLine($"AggregateException: {ex.Message}");
            }
            throw;
        }
        finally
        {
            snapshot.Dispose();
        }
    }

    [TestMethod]
    [ExpectedException(typeof(AggregateException))]
    public void TestMultiThreadLevelDbSnapshotPut()
    {
        using var store = levelDbStore.GetStore(path_leveldb);

        object locker = new();
        var snapshot = store.GetSnapshot();

        var testKey = new byte[] { 0x01, 0x02, 0x03 };

        var tasks = new Task[2];
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

        var tasks = new Task[100];
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

    #endregion
}
