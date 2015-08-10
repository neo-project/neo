using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.IO.Caching
{
    internal abstract class Cache<TKey, TValue> : ICollection<TValue>, IDisposable
    {
        protected Dictionary<TKey, CacheItem<TKey, TValue>> InnerDictionary = new Dictionary<TKey, CacheItem<TKey, TValue>>();
        private int max_capacity;

        public virtual TValue this[TKey key]
        {
            get
            {
                return InnerDictionary[key].Value;
            }
        }

        public virtual int Count
        {
            get
            {
                return InnerDictionary.Count;
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

        public virtual void Add(TValue item)
        {
            TKey key = GetKeyForItem(item);
            if (InnerDictionary.ContainsKey(key))
            {
                InnerDictionary[key].Update();
            }
            else
            {
                if (InnerDictionary.Count >= max_capacity)
                {
                    //TODO: 对PLINQ查询进行性能测试，以便确定此处使用何种算法更优（并行或串行）
                    foreach (CacheItem<TKey, TValue> item_del in InnerDictionary.Values.AsParallel().OrderBy(p => p.LastUpdate).Take(InnerDictionary.Count - max_capacity + 1))
                    {
                        RemoveInternal(item_del);
                    }
                }
                InnerDictionary.Add(key, new CacheItem<TKey, TValue>(key, item));
            }
        }

        public virtual void Clear()
        {
            foreach (CacheItem<TKey, TValue> item_del in InnerDictionary.Values.ToArray())
            {
                RemoveInternal(item_del);
            }
        }

        public virtual bool Contains(TKey key)
        {
            return InnerDictionary.ContainsKey(key);
        }

        public bool Contains(TValue item)
        {
            return Contains(GetKeyForItem(item));
        }

        public virtual void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException();
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (arrayIndex + InnerDictionary.Count > array.Length) throw new ArgumentException();
            foreach (TValue item in InnerDictionary.Values.Select(p => p.Value))
            {
                array[arrayIndex++] = item;
            }
        }

        public void Dispose()
        {
            Clear();
        }

        public virtual IEnumerator<TValue> GetEnumerator()
        {
            return InnerDictionary.Values.Select(p => p.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected abstract TKey GetKeyForItem(TValue item);

        public virtual bool Remove(TKey key)
        {
            if (!InnerDictionary.ContainsKey(key)) return false;
            RemoveInternal(InnerDictionary[key]);
            return true;
        }

        public bool Remove(TValue item)
        {
            return Remove(GetKeyForItem(item));
        }

        private void RemoveInternal(CacheItem<TKey, TValue> item)
        {
            InnerDictionary.Remove(item.Key);
            IDisposable disposable = item.Value as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
