// Copyright (C) 2015-2025 The Neo Project.
//
// Benchmarks.PoolItemComparer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using BenchmarkDotNet.Attributes;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Benchmark
{
    [MemoryDiagnoser]
    public class Benchmarks_PoolItemComparer
    {
        private const int NumItems = 10000;
        private TestPoolItem[] poolItems = Array.Empty<TestPoolItem>();
        private readonly SortedSet<TestPoolItem> _reversedSortedSet = new(ReverseTestPoolItemComparer.Instance);
        private readonly SortedSet<TestPoolItem> _directSortedSet = new(DirectTestPoolItemComparer.Instance);

        // A class that mimics the behavior of the internal PoolItem for testing
        public class TestPoolItem : IComparable<TestPoolItem>
        {
            public readonly Transaction Tx;
            public readonly DateTime Timestamp;
            public DateTime LastBroadcastTimestamp;

            public TestPoolItem(Transaction tx)
            {
                Tx = tx;
                Timestamp = DateTime.UtcNow;
                LastBroadcastTimestamp = Timestamp;
            }

            public int CompareTo(TestPoolItem? other)
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

        private sealed class ReverseTestPoolItemComparer : IComparer<TestPoolItem>
        {
            public static readonly ReverseTestPoolItemComparer Instance = new ReverseTestPoolItemComparer();

            public int Compare(TestPoolItem? x, TestPoolItem? y)
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

        private sealed class DirectTestPoolItemComparer : IComparer<TestPoolItem>
        {
            public static readonly DirectTestPoolItemComparer Instance = new DirectTestPoolItemComparer();

            public int Compare(TestPoolItem? x, TestPoolItem? y)
            {
                if (x == null)
                {
                    if (y == null) return 0;
                    return -1;
                }

                return x.CompareTo(y);
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            // Create test pool items with random fees
            Random random = new Random(42); // Fixed seed for reproducibility
            poolItems = new TestPoolItem[NumItems];

            // Clear the sorted sets
            _reversedSortedSet.Clear();
            _directSortedSet.Clear();

            for (int i = 0; i < NumItems; i++)
            {
                // Create transaction with random fee
                var tx = new Transaction
                {
                    NetworkFee = random.Next(1, 10000),
                    SystemFee = random.Next(1, 10000),
                    Script = BitConverter.GetBytes(i), // Unique script
                    Signers = Array.Empty<Signer>(),
                    Witnesses = new[] { new Witness
                    {
                        InvocationScript = Array.Empty<byte>(),
                        VerificationScript = Array.Empty<byte>()
                    }}
                };

                // Set high priority for some transactions
                if (random.Next(10) == 0) // 10% of transactions get high priority
                {
                    tx.Attributes = new TransactionAttribute[] { new HighPriorityAttribute() };
                }
                else
                {
                    tx.Attributes = Array.Empty<TransactionAttribute>();
                }

                // Create pool item
                poolItems[i] = new TestPoolItem(tx);
            }
        }

        [Benchmark]
        public void Insert_ReversedSortedSet()
        {
            // Clear and refill the set
            _reversedSortedSet.Clear();
            foreach (var item in poolItems)
            {
                _reversedSortedSet.Add(item);
            }
        }

        [Benchmark]
        public void Insert_DirectSortedSet()
        {
            // Clear and refill the set
            _directSortedSet.Clear();
            foreach (var item in poolItems)
            {
                _directSortedSet.Add(item);
            }
        }

        [Benchmark]
        public List<TestPoolItem> GetSorted_ReversedSortedSet()
        {
            // First ensure the set is filled
            if (_reversedSortedSet.Count == 0)
            {
                foreach (var item in poolItems)
                {
                    _reversedSortedSet.Add(item);
                }
            }

            // Get items in sorted order (reversed)
            return _reversedSortedSet.ToList();
        }

        [Benchmark]
        public List<TestPoolItem> GetSorted_DirectSortedSet()
        {
            // First ensure the set is filled
            if (_directSortedSet.Count == 0)
            {
                foreach (var item in poolItems)
                {
                    _directSortedSet.Add(item);
                }
            }

            // Get items in sorted order (direct)
            return _directSortedSet.Reverse().ToList(); // Reverse to match the behavior of reversed set
        }

        [Benchmark]
        public TestPoolItem GetTop_ReversedSortedSet()
        {
            // First ensure the set is filled
            if (_reversedSortedSet.Count == 0)
            {
                foreach (var item in poolItems)
                {
                    _reversedSortedSet.Add(item);
                }
            }

            // Get the first item (highest priority)
            return _reversedSortedSet.First();
        }

        [Benchmark]
        public TestPoolItem GetTop_DirectSortedSet()
        {
            // First ensure the set is filled
            if (_directSortedSet.Count == 0)
            {
                foreach (var item in poolItems)
                {
                    _directSortedSet.Add(item);
                }
            }

            // Get the last item (highest priority due to reverse order)
            return _directSortedSet.Last();
        }
    }
}

#nullable disable
