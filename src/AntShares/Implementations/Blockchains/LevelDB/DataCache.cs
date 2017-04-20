using AntShares.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.Implementations.Blockchains.LevelDB
{
    internal class DataCache<TKey, TValue>
        where TKey : ISerializable
        where TValue : class, ISerializable, new()
    {
        private DB db;
        private DataEntryPrefix prefix;
        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
        private HashSet<TKey> deleted = new HashSet<TKey>();

        public TValue this[TKey key]
        {
            get
            {
                if (dictionary.ContainsKey(key)) return dictionary[key];
                if (deleted.Contains(key)) throw new KeyNotFoundException();
                TValue value = db.Get<TValue>(ReadOptions.Default, prefix, key);
                dictionary.Add(key, value);
                return value;
            }
        }

        public DataCache(DB db, DataEntryPrefix prefix)
        {
            this.db = db;
            this.prefix = prefix;
        }

        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);
            deleted.Remove(key);
        }

        public void Commit(WriteBatch batch)
        {
            foreach (var pair in dictionary)
                batch.Put(prefix, pair.Key, pair.Value);
            foreach (TKey key in deleted)
                batch.Delete(prefix, key);
        }

        public void Delete(TKey key)
        {
            dictionary.Remove(key);
            deleted.Add(key);
        }

        public void DeleteWhere(Func<TKey, TValue, bool> predicate)
        {
            TKey[] keys = dictionary.Where(p => predicate(p.Key, p.Value)).Select(p => p.Key).ToArray();
            foreach (TKey key in keys)
                Delete(key);
        }

        public TValue GetOrAdd(TKey key, Func<TValue> factory)
        {
            if (dictionary.ContainsKey(key)) return dictionary[key];
            TValue value;
            if (deleted.Contains(key))
            {
                value = factory();
                deleted.Remove(key);
            }
            else
            {
                value = db.TryGet<TValue>(ReadOptions.Default, prefix, key);
                if (value == null) value = factory();
            }
            dictionary.Add(key, value);
            return value;
        }

        public TValue TryGet(TKey key)
        {
            if (dictionary.ContainsKey(key)) return dictionary[key];
            if (deleted.Contains(key)) return null;
            TValue value = db.TryGet<TValue>(ReadOptions.Default, prefix, key);
            if (value == null)
                return null;
            dictionary.Add(key, value);
            return value;
        }
    }
}
