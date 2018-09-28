using Neo.Network.P2P.Payloads;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Ledger
{
    internal class MemPool : IReadOnlyCollection<Transaction>
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

        private readonly ConcurrentDictionary<UInt256, PoolItem> mem_pool = new ConcurrentDictionary<UInt256, PoolItem>();

        public int Count => mem_pool.Count;

        public void Clear()
        {
            mem_pool.Clear();
        }

        public bool ContainsKey(UInt256 hash)
        {
            return mem_pool.ContainsKey(hash);
        }

        public IEnumerator<Transaction> GetEnumerator()
        {
            return mem_pool.Select(p => p.Value.Transaction).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void RemoveOldFree(DateTime time)
        {
            UInt256[] hashes = mem_pool.Where(p => p.Value.Timestamp < time && p.Value.Transaction.NetworkFee == Fixed8.Zero)
                .Select(p => p.Key)
                .ToArray();
            foreach (UInt256 hash in hashes)
                mem_pool.TryRemove(hash, out _);
        }

        public bool TryAdd(UInt256 hash, Transaction tx)
        {
            return mem_pool.TryAdd(hash, new PoolItem(tx));
        }

        public bool TryRemove(UInt256 hash, out Transaction tx)
        {
            if (mem_pool.TryRemove(hash, out PoolItem item))
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
            if (mem_pool.TryGetValue(hash, out PoolItem item))
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
