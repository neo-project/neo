using System;
using System.Collections.Generic;

namespace AntShares.Data
{
    internal class MultiValueDictionary<TKey, TValue> : Dictionary<TKey, HashSet<TValue>>
    {
        private Func<TKey, HashSet<TValue>> constructor;

        public MultiValueDictionary()
            : this(k => new HashSet<TValue>())
        {
        }

        public MultiValueDictionary(Func<TKey, HashSet<TValue>> constructor)
        {
            this.constructor = constructor;
        }

        public bool Add(TKey key, TValue item)
        {
            EnsureKey(key);
            return this[key].Add(item);
        }

        public bool AddEmpty(TKey key)
        {
            if (ContainsKey(key)) return false;
            Add(key, new HashSet<TValue>());
            return true;
        }

        public void AddRange(TKey key, IEnumerable<TValue> items)
        {
            EnsureKey(key);
            this[key].UnionWith(items);
        }

        private void EnsureKey(TKey key)
        {
            if (!ContainsKey(key))
            {
                Add(key, constructor(key));
            }
        }

        public bool Remove(TKey key, TValue item)
        {
            EnsureKey(key);
            return this[key].Remove(item);
        }
    }
}
