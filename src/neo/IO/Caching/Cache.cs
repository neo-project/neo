// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory or 
// the project http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            public readonly TKey Key;
            public readonly TValue Value;
            public readonly DateTime Time;

            public CacheItem(TKey key, TValue value)
            {
                this.Key = key;
                this.Value = value;
                this.Time = TimeProvider.Current.UtcNow;
            }
        }

        protected readonly ReaderWriterLockSlim RwSyncRootLock = new(LockRecursionPolicy.SupportsRecursion);
        protected readonly Dictionary<TKey, CacheItem> InnerDictionary = new();
        private readonly int max_capacity;

        public TValue this[TKey key]
        {
            get
            {
                RwSyncRootLock.EnterReadLock();
                try
                {
                    if (!InnerDictionary.TryGetValue(key, out CacheItem item)) throw new KeyNotFoundException();
                    OnAccess(item);
                    return item.Value;
                }
                finally
                {
                    RwSyncRootLock.ExitReadLock();
                }
            }
        }

        public int Count
        {
            get
            {
                RwSyncRootLock.EnterReadLock();
                try
                {
                    return InnerDictionary.Count;
                }
                finally
                {
                    RwSyncRootLock.ExitReadLock();
                }
            }
        }

        public bool IsReadOnly => false;

        public Cache(int max_capacity)
        {
            this.max_capacity = max_capacity;
        }

        public void Add(TValue item)
        {
            TKey key = GetKeyForItem(item);
            RwSyncRootLock.EnterWriteLock();
            try
            {
                AddInternal(key, item);
            }
            finally
            {
                RwSyncRootLock.ExitWriteLock();
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
                    //TODO: Perform a performance test on the PLINQ query to determine which algorithm is better here (parallel or not)
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
            RwSyncRootLock.EnterWriteLock();
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
                RwSyncRootLock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            RwSyncRootLock.EnterWriteLock();
            try
            {
                foreach (CacheItem item_del in InnerDictionary.Values.ToArray())
                {
                    RemoveInternal(item_del);
                }
            }
            finally
            {
                RwSyncRootLock.ExitWriteLock();
            }
        }

        public bool Contains(TKey key)
        {
            RwSyncRootLock.EnterReadLock();
            try
            {
                if (!InnerDictionary.TryGetValue(key, out CacheItem cacheItem)) return false;
                OnAccess(cacheItem);
                return true;
            }
            finally
            {
                RwSyncRootLock.ExitReadLock();
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
            RwSyncRootLock.Dispose();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            RwSyncRootLock.EnterReadLock();
            try
            {
                foreach (TValue item in InnerDictionary.Values.Select(p => p.Value))
                {
                    yield return item;
                }
            }
            finally
            {
                RwSyncRootLock.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected abstract TKey GetKeyForItem(TValue item);

        public bool Remove(TKey key)
        {
            RwSyncRootLock.EnterWriteLock();
            try
            {
                if (!InnerDictionary.TryGetValue(key, out CacheItem cacheItem)) return false;
                RemoveInternal(cacheItem);
                return true;
            }
            finally
            {
                RwSyncRootLock.ExitWriteLock();
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
            RwSyncRootLock.EnterReadLock();
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
                RwSyncRootLock.ExitReadLock();
            }
            item = default;
            return false;
        }
    }
}
