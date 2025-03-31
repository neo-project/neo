// Copyright (C) 2015-2025 The Neo Project.
//
// Cache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
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
    public abstract class Cache<TKey, TValue>(int maxCapacity, IEqualityComparer<TKey>? comparer = null)
        : ICollection<TValue>, IDisposable where TKey : notnull
    {
        protected record class CacheItem(TKey Key, TValue Value)
        {
            public readonly DateTime Time = DateTime.UtcNow;
        }

        protected readonly ReaderWriterLockSlim RwSyncRootLock = new(LockRecursionPolicy.SupportsRecursion);
        protected readonly Dictionary<TKey, CacheItem> InnerDictionary = new(comparer);

        public TValue this[TKey key]
        {
            get
            {
                RwSyncRootLock.EnterReadLock();
                try
                {
                    if (!InnerDictionary.TryGetValue(key, out var cached)) throw new KeyNotFoundException();
                    OnAccess(cached);
                    return cached.Value;
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

        public void Add(TValue item)
        {
            var key = GetKeyForItem(item);
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
            if (InnerDictionary.TryGetValue(key, out var cached))
            {
                OnAccess(cached);
            }
            else
            {
                if (InnerDictionary.Count >= maxCapacity)
                {
                    var removedCount = InnerDictionary.Count - maxCapacity + 1;
                    foreach (var toDelete in InnerDictionary.Values.OrderBy(p => p.Time).Take(removedCount))
                    {
                        RemoveInternal(toDelete);
                    }
                }
                InnerDictionary.Add(key, new(key, item));
            }
        }

        public void AddRange(IEnumerable<TValue> items)
        {
            RwSyncRootLock.EnterWriteLock();
            try
            {
                foreach (var item in items)
                {
                    var key = GetKeyForItem(item);
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
                foreach (var toDelete in InnerDictionary.Values.ToArray())
                {
                    RemoveInternal(toDelete);
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
                if (!InnerDictionary.TryGetValue(key, out var cached)) return false;
                OnAccess(cached);
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

        public void CopyTo(TValue[] array, int startIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (startIndex + InnerDictionary.Count > array.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            foreach (var item in this)
            {
                array[startIndex++] = item;
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
                foreach (var item in InnerDictionary.Values.Select(p => p.Value))
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
                if (!InnerDictionary.TryGetValue(key, out var cached)) return false;
                RemoveInternal(cached);
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
                if (InnerDictionary.TryGetValue(key, out var cached))
                {
                    OnAccess(cached);
                    item = cached.Value;
                    return true;
                }
            }
            finally
            {
                RwSyncRootLock.ExitReadLock();
            }
            item = default!;
            return false;
        }
    }
}
