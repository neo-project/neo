// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.MemoryPool.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neo.Benchmark
{
    /// <summary>
    /// A specialized MemoryPool for benchmarking that exposes the internal TryAdd method
    /// </summary>
    public class BenchmarkMemoryPool : MemoryPool
    {
        public BenchmarkMemoryPool() : base(MockNeoSystem())
        {
        }

        private static NeoSystem MockNeoSystem()
        {
            // Use reflection to create a minimal NeoSystem without initializing all components
            var constructor = typeof(NeoSystem).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null, new Type[] { typeof(ProtocolSettings) }, null);

            var settings = ProtocolSettings.Default with { MemoryPoolMaxTransactions = 1000 };
            return (NeoSystem)constructor.Invoke(new object[] { settings });
        }

        public bool TryAddPublic(Transaction tx, DataCache snapshot)
        {
            var method = typeof(MemoryPool).GetMethod("TryAdd",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)method.Invoke(this, new object[] { tx, snapshot });
        }
    }

    /// <summary>
    /// A class that mimics the behavior of the internal PoolItem for MemoryPool benchmarks
    /// </summary>
    public class TestMemPoolItem : IComparable<TestMemPoolItem>
    {
        public readonly Transaction Tx;
        public readonly DateTime Timestamp;
        public DateTime LastBroadcastTimestamp;

        public TestMemPoolItem(Transaction tx)
        {
            Tx = tx;
            Timestamp = DateTime.UtcNow;
            LastBroadcastTimestamp = Timestamp;
        }

        public int CompareTo(TestMemPoolItem other)
        {
            if (other == null) return 1;

            var ret = (Tx.GetAttribute<HighPriorityAttribute>() != null)
                .CompareTo(other.Tx.GetAttribute<HighPriorityAttribute>() != null);
            if (ret != 0) return ret;

            // Fees sorted ascending
            ret = Tx.FeePerByte.CompareTo(other.Tx.FeePerByte);
            if (ret != 0) return ret;
            ret = Tx.NetworkFee.CompareTo(other.Tx.NetworkFee);
            if (ret != 0) return ret;

            // Transaction hash sorted descending
            return other.Tx.Hash.CompareTo(Tx.Hash);
        }
    }

    /// <summary>
    /// Reverse comparer for TestMemPoolItem, mimics the internal ReversePoolItemComparer
    /// </summary>
    public class ReverseTestMemPoolItemComparer : IComparer<TestMemPoolItem>
    {
        public static readonly ReverseTestMemPoolItemComparer Instance = new ReverseTestMemPoolItemComparer();

        public int Compare(TestMemPoolItem x, TestMemPoolItem y)
        {
            if (y == null)
            {
                if (x == null) return 0;
                return -1;
            }

            // Reverse value
            return y.CompareTo(x);
        }
    }

    [MemoryDiagnoser]
    public class Benchmarks_MemoryPool
    {
        private const int NumTransactions = 200;
        private readonly Random random = new(42); // Fixed seed for reproducibility
        private readonly UInt160 senderAccount = UInt160.Zero;
        private List<TestMemPoolItem> poolItems;
        private SortedSet<TestMemPoolItem> reverseOrderedPoolItems;

        [GlobalSetup]
        public void Setup()
        {
            // Generate test pool items with varying fees
            poolItems = new List<TestMemPoolItem>(NumTransactions);

            for (int i = 0; i < NumTransactions; i++)
            {
                var tx = new Transaction
                {
                    Version = 0,
                    Nonce = (uint)random.Next(),
                    ValidUntilBlock = (uint)random.Next(),
                    Signers = new[] { new Signer { Account = senderAccount, Scopes = WitnessScope.None } },
                    Attributes = Array.Empty<TransactionAttribute>(),
                    Script = new byte[] { (byte)OpCode.RET },
                    Witnesses = Array.Empty<Witness>(),
                    // Set different fees to create variation
                    NetworkFee = (i + 1) * 100,
                    SystemFee = (NumTransactions - i) * 100
                };

                // Create a pool item with the transaction
                poolItems.Add(new TestMemPoolItem(tx));
            }

            // Pre-create the sorted set with ReverseTestMemPoolItemComparer
            reverseOrderedPoolItems = new SortedSet<TestMemPoolItem>(ReverseTestMemPoolItemComparer.Instance);
        }

        [Benchmark]
        public void AddTransactions()
        {
            // Clear the set for each benchmark run
            reverseOrderedPoolItems.Clear();

            // Add all transactions to the sorted set
            foreach (var item in poolItems)
            {
                reverseOrderedPoolItems.Add(item);
            }
        }

        [Benchmark]
        public void RetrieveSortedTransactions()
        {
            // Clear and add items
            reverseOrderedPoolItems.Clear();
            foreach (var item in poolItems)
            {
                reverseOrderedPoolItems.Add(item);
            }

            // Retrieve all transactions in reverse order (simulating GetSortedVerifiedTransactions)
            var result = new List<Transaction>(poolItems.Count);
            foreach (var item in reverseOrderedPoolItems)
            {
                result.Add(item.Tx);
            }
        }
    }
}
