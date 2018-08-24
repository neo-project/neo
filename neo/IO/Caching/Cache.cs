using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Neo.IO.Caching
{
    internal abstract class Cache<TKey, TValue> : ICollection<TValue>, IDisposable
    {
        protected class CacheItem
        {
            public TKey Key;
            public TValue Value;
            public DateTime Time;

            public CacheItem(TKey key, TValue value)
            {
                this.Key = key;
                this.Value = value;
                this.Time = DateTime.Now;
            }
        }

        protected readonly ReaderWriterLock RwSyncRootLock = new ReaderWriterLock();
        protected readonly Dictionary<TKey, CacheItem> InnerDictionary = new Dictionary<TKey, CacheItem>();
        private readonly int max_capacity;

        public TValue this[TKey key]
        {
            get
            {
                RwSyncRootLock.AcquireReaderLock(-1);
                try
                {
                    if (!InnerDictionary.TryGetValue(key, out CacheItem item)) throw new KeyNotFoundException();
                    OnAccess(item);
                    return item.Value;
                }
                finally
                {
                    RwSyncRootLock.ReleaseReaderLock();
                }
            }
        }

        public int Count
        {
            get
            {
                RwSyncRootLock.AcquireReaderLock(-1);
                try
                {
                    return InnerDictionary.Count;
                }
                finally
                {
                    RwSyncRootLock.ReleaseReaderLock();
                }
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
            RwSyncRootLock.AcquireWriterLock(-1);
            try
            {
                AddInternal(key, item);
            }
            finally
            {
                RwSyncRootLock.ReleaseWriterLock();
            }
        }

        private void AddInternal(TKey key, TValue item)
        {
            if (InnerDictionary.TryGetValue(key, out CacheItem cacheItem))
            {
                OnAccess(cacheItem);
            }
            else
            {
                if (InnerDictionary.Count >= max_capacity)
                {
                    //TODO: 对PLINQ查询进行性能测试，以便确定此处使用何种算法更优（并行或串行）
                    foreach (CacheItem item_del in InnerDictionary.Values.AsParallel().OrderBy(p => p.Time).Take(InnerDictionary.Count - max_capacity + 1))
                    {
                        RemoveInternal(item_del);
                    }
                }
                InnerDictionary.Add(key, new CacheItem(key, item));
            }
        }

        public void AddRange(IEnumerable<TValue> items)
        {
            RwSyncRootLock.AcquireWriterLock(-1);
            try
            {
                foreach (TValue item in items)
                {
                    TKey key = GetKeyForItem(item);
                    AddInternal(key, item);
                }
            }
            finally
            {
                RwSyncRootLock.ReleaseWriterLock();
            }
        }

        public void Clear()
        {
            RwSyncRootLock.AcquireWriterLock(-1);
            try
            {
                foreach (CacheItem item_del in InnerDictionary.Values.ToArray())
                {
                    RemoveInternal(item_del);
                }
            }
            finally
            {
                RwSyncRootLock.ReleaseWriterLock();
            }
        }

        public bool Contains(TKey key)
        {
            RwSyncRootLock.AcquireReaderLock(-1);
            try
            {
                if (!InnerDictionary.TryGetValue(key, out CacheItem cacheItem)) return false;
                OnAccess(cacheItem);
                return true;
            }
            finally
            {
                RwSyncRootLock.ReleaseReaderLock();
            }
        }

        public bool Contains(TValue item)
        {
            return Contains(GetKeyForItem(item));
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException();
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (arrayIndex + InnerDictionary.Count > array.Length) throw new ArgumentException();
            foreach (TValue item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        public void Dispose()
        {
            Clear();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            RwSyncRootLock.AcquireReaderLock(-1);
            try
            {
                foreach (TValue item in InnerDictionary.Values.Select(p => p.Value))
                {
                    yield return item;
                }
            }
            finally
            {
                RwSyncRootLock.ReleaseReaderLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected abstract TKey GetKeyForItem(TValue item);

        public bool Remove(TKey key)
        {
            RwSyncRootLock.AcquireWriterLock(-1);
            try
            {
                if (!InnerDictionary.TryGetValue(key, out CacheItem cacheItem)) return false;
                RemoveInternal(cacheItem);
                return true;
            }
            finally
            {
                RwSyncRootLock.ReleaseWriterLock();
            }
        }

        protected abstract void OnAccess(CacheItem item);

        public bool Remove(TValue item)
        {
            return Remove(GetKeyForItem(item));
        }

        private void RemoveInternal(CacheItem item)
        {
            InnerDictionary.Remove(item.Key);
            if (item.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        public bool TryGet(TKey key, out TValue item)
        {
            RwSyncRootLock.AcquireReaderLock(-1);
            try
            {
                if (InnerDictionary.TryGetValue(key, out CacheItem cacheItem))
                {
                    OnAccess(cacheItem);
                    item = cacheItem.Value;
                    return true;
                }
            }
            finally
            {
                RwSyncRootLock.ReleaseReaderLock();
            }
            item = default(TValue);
            return false;
        }
    }
}
