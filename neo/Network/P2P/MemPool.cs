using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Neo.Network.P2P.Payloads;
using System.Collections.Concurrent;

namespace Neo.Network.P2P
{
    class PoolItem
    {
        public readonly Transaction tx;
        public readonly DateTime timestamp;

        internal PoolItem(Transaction value)
        {
            tx = value;
            timestamp = DateTime.UtcNow;
        }
    }

    class MemPool : IEnumerable<PoolItem>
    {
        private readonly ConcurrentDictionary<UInt256, PoolItem> mem_pool = new ConcurrentDictionary<UInt256, PoolItem>();
        public IEnumerable<Transaction> Values => mem_pool.Select(p => p.Value.tx);
        public int Count => mem_pool.Count;

        internal bool ContainsKey(UInt256 hash) => mem_pool.ContainsKey(hash);

        public IEnumerator<PoolItem> GetEnumerator() => mem_pool.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => mem_pool.Values.GetEnumerator();
      
        public IEnumerable<Transaction> GetValuesBy(Func<PoolItem, bool> f)
        {
            return mem_pool.Values.Where(f).Select(p => p.tx);
        }

        internal void TryAdd(UInt256 hash, Transaction tx)
        {
            mem_pool.TryAdd(hash, new PoolItem(tx));
        }

        internal void TryRemove(UInt256 hash, out PoolItem _)
        {
            mem_pool.TryRemove(hash, out _);
        }

        public void Clear()
        {
            mem_pool.Clear();
        }

        internal bool TryGetValue(UInt256 hash, out Transaction item)
        {
            if (mem_pool.TryGetValue(hash, out var i))
            {
                item = i.tx;
                return true;
            }
            else
            {
                item = null;
                return false;
            }
        }
    }
}