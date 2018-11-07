using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Neo.Ledger
{
    internal class MemoryPool : IReadOnlyCollection<Transaction>
    {
        private class PoolItem
        {
            public readonly Transaction Transaction;
            public readonly DateTime Timestamp;

            public PoolItem(Transaction tx)
            {
                Transaction = tx;
                Timestamp = DateTime.UtcNow;
            }
        }

        private readonly ConcurrentDictionary<UInt256, PoolItem> _mem_pool_fee = new ConcurrentDictionary<UInt256, PoolItem>();
        private readonly ConcurrentDictionary<UInt256, PoolItem> _mem_pool_free = new ConcurrentDictionary<UInt256, PoolItem>();

        public int Capacity { get; }
        public int Count => _mem_pool_fee.Count + _mem_pool_free.Count;

        public MemoryPool(int capacity)
        {
            Capacity = capacity;
        }

        public void Clear()
        {
            _mem_pool_free.Clear();
            _mem_pool_fee.Clear();
        }

        public bool ContainsKey(UInt256 hash) => _mem_pool_free.ContainsKey(hash) || _mem_pool_fee.ContainsKey(hash);

        public IEnumerator<Transaction> GetEnumerator()
        {
            return
                _mem_pool_fee.Select(p => p.Value.Transaction)
                .Concat(_mem_pool_free.Select(p => p.Value.Transaction))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        static void RemoveLowestFee(ConcurrentDictionary<UInt256, PoolItem> pool, int count)
        {
            if (count <= 0) return;
            if (count >= pool.Count)
            {
                pool.Clear();
            }
            else
            {
                UInt256[] delete = pool.AsParallel()
                    .OrderBy(p => p.Value.Transaction.NetworkFee / p.Value.Transaction.Size)
                    .ThenBy(p => p.Value.Transaction.NetworkFee)
                    .ThenBy(p => new BigInteger(p.Key.ToArray()))
                    .Take(count)
                    .Select(p => p.Key)
                    .ToArray();

                foreach (UInt256 hash in delete)
                {
                    pool.TryRemove(hash, out _);
                }
            }
        }

        static void RemoveOldest(ConcurrentDictionary<UInt256, PoolItem> pool, DateTime time)
        {
            UInt256[] hashes = pool
                .Where(p => p.Value.Timestamp < time)
                .Select(p => p.Key)
                .ToArray();

            foreach (UInt256 hash in hashes)
            {
                pool.TryRemove(hash, out _);
            }
        }

        public bool TryAdd(UInt256 hash, Transaction tx)
        {
            ConcurrentDictionary<UInt256, PoolItem> pool;

            if (tx.IsLowPriority)
            {
                pool = _mem_pool_free;
            }
            else
            {
                pool = _mem_pool_fee;
            }

            pool.TryAdd(hash, new PoolItem(tx));

            if (Count > Capacity)
            {
                RemoveOldest(_mem_pool_free, DateTime.UtcNow.AddSeconds(-Blockchain.SecondsPerBlock * 20));

                var exceed = Count - Capacity;

                if (exceed > 0)
                {
                    RemoveLowestFee(_mem_pool_free, exceed);
                    exceed = Count - Capacity;

                    if (exceed > 0)
                    {
                        RemoveLowestFee(_mem_pool_fee, exceed);
                    }
                }
            }

            return pool.ContainsKey(hash);
        }

        public bool TryRemove(UInt256 hash, out Transaction tx)
        {
            if (_mem_pool_free.TryRemove(hash, out PoolItem item))
            {
                tx = item.Transaction;
                return true;
            }
            else if (_mem_pool_free.TryRemove(hash, out item))
            {
                tx = item.Transaction;
                return true;
            }
            else
            {
                tx = null;
                return false;
            }
        }

        public bool TryGetValue(UInt256 hash, out Transaction tx)
        {
            if (_mem_pool_free.TryGetValue(hash, out PoolItem item))
            {
                tx = item.Transaction;
                return true;
            }
            else if (_mem_pool_fee.TryGetValue(hash, out item))
            {
                tx = item.Transaction;
                return true;
            }
            else
            {
                tx = null;
                return false;
            }
        }
    }
}
