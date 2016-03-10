using System;
using System.Collections.Generic;

namespace AntShares.Implementations.Blockchains.LevelDB
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

    internal class MultiValueDictionary<TKey, TValueKey, TValueData> : Dictionary<TKey, Dictionary<TValueKey, TValueData>>
    {
        private Func<TKey, Dictionary<TValueKey, TValueData>> constructor;

        public MultiValueDictionary()
            : this(k => new Dictionary<TValueKey, TValueData>())
        {
        }

        public MultiValueDictionary(Func<TKey, Dictionary<TValueKey, TValueData>> constructor)
        {
            this.constructor = constructor;
        }

        public bool Add(TKey key, TValueKey itemKey, TValueData itemData)
        {
            EnsureKey(key);
            if (this[key].ContainsKey(itemKey)) return false;
            this[key].Add(itemKey, itemData);
            return true;
        }

        public bool AddEmpty(TKey key)
        {
            if (ContainsKey(key)) return false;
            Add(key, new Dictionary<TValueKey, TValueData>());
            return true;
        }

        private void EnsureKey(TKey key)
        {
            if (!ContainsKey(key))
            {
                Add(key, constructor(key));
            }
        }

        public bool Remove(TKey key, TValueKey itemKey)
        {
            EnsureKey(key);
            return this[key].Remove(itemKey);
        }
    }
}
