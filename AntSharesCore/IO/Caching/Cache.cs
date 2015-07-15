using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.IO.Caching
{
    internal abstract class Cache<TKey, TValue> : ICollection<TValue>, IDisposable
    {
        private class CacheItem
        {
            public TKey Key;
            public TValue Value;
            public DateTime LastUpdate;

            public CacheItem(TKey key, TValue value)
            {
                this.Key = key;
                this.Value = value;
            }

            public void Update()
            {
                LastUpdate = DateTime.Now;
            }
        }

        private Dictionary<TKey, CacheItem> dictionary = new Dictionary<TKey, CacheItem>();
        private int max_capacity;

        public int Count
        {
            get
            {
                return dictionary.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public Cache(int max_capacity)
        {
            this.max_capacity = max_capacity;
        }

        public void Add(TValue item)
        {
            TKey key = GetKeyForItem(item);
            if (dictionary.ContainsKey(key))
            {
                dictionary[key].Update();
            }
            else
            {
                if (dictionary.Count >= max_capacity)
                {
                    //TODO: 对PLINQ查询进行性能测试，以便确定此处使用何种算法更优（并行或串行）
                    foreach (CacheItem item_del in dictionary.Values.AsParallel().OrderBy(p => p.LastUpdate).Take(dictionary.Count - max_capacity + 1))
                    {
                        RemoveInternal(item_del);
                    }
                }
                dictionary.Add(key, new CacheItem(key, item));
            }
        }

        public void Clear()
        {
            foreach (CacheItem item_del in dictionary.Values.ToArray())
            {
                RemoveInternal(item_del);
            }
        }

        public bool Contains(TValue item)
        {
            TKey key = GetKeyForItem(item);
            return dictionary.ContainsKey(key);
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException();
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (arrayIndex + dictionary.Count > array.Length) throw new ArgumentException();
            foreach (TValue item in dictionary.Values.Select(p => p.Value))
            {
                array[arrayIndex++] = item;
            }
        }

        public void Dispose()
        {
            Clear();
        }

        protected abstract TKey GetKeyForItem(TValue item);

        public bool Remove(TValue item)
        {
            TKey key = GetKeyForItem(item);
            if (!dictionary.ContainsKey(key)) return false;
            RemoveInternal(dictionary[key]);
            return true;
        }

        private void RemoveInternal(CacheItem item)
        {
            dictionary.Remove(item.Key);
            IDisposable disposable = item.Value as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return dictionary.Values.Select(p => p.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
