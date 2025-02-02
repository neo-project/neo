// Copyright (C) 2015-2025 The Neo Project.
//
// StorageCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Neo.Persistence
{
    public abstract partial class StorageCache : IReadOnlyStoreView
    {

#if NET9_0_OR_GREATER
        private readonly System.Threading.Lock _lockObj = new();
#else
        private readonly object _lockObj = new();
#endif

        private readonly Dictionary<StorageKey, CacheEntry> _cachedItems = new(StorageKeyEqualityComparer.Instance);

        #region IReadOnlyStoreView

        public StorageItem this[[DisallowNull] StorageKey key]
        {
            [return: MaybeNull]
            get
            {
                lock (_lockObj)
                {
                    if (_cachedItems.TryGetValue(key, out var cachedEntry))
                    {
                        if (cachedEntry.State == TrackState.Deleted || cachedEntry.State == TrackState.NotFound)
                            throw new KeyNotFoundException();
                    }
                    else
                    {
                        var storeEntry = TryGetInternal(key);

                        if (storeEntry is null)
                            throw new KeyNotFoundException();
                        else
                        {
                            cachedEntry = new(key, null, TrackState.None);
                            _cachedItems.Add(key, cachedEntry);
                        }
                    }

                    return cachedEntry.Value;
                }
            }
        }

        public bool Contains(StorageKey key)
        {
            throw new NotImplementedException();
        }

        public bool TryGet(StorageKey key, out StorageItem item)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected abstract bool ContainsInternal([DisallowNull] StorageKey key);

        protected abstract void AddInternal([DisallowNull] StorageKey key, [DisallowNull] StorageItem value);

        protected abstract void UpdateInternal([DisallowNull] StorageKey key, [DisallowNull] StorageItem value);

        protected abstract IEnumerable<(StorageKey Key, StorageItem Value)> SeekInternal([AllowNull] byte[] keyOrPrefix, SeekDirection direction);


        [return: MaybeNull]
        protected abstract StorageItem TryGetInternal([DisallowNull] StorageKey key);

        [return: NotNull]
        protected abstract StorageItem GetInternal([DisallowNull] StorageKey key);

        protected abstract void DeleteInternal([DisallowNull] StorageKey key);


        public void Add([DisallowNull] StorageKey key, [DisallowNull] StorageItem value)
        {
            lock (_lockObj)
            {
                if (_cachedItems.TryGetValue(key, out var cachedItem) == false)
                    _cachedItems[key] = new(key, value, TrackState.Added);
                else
                {
                    cachedItem.Value = value;
                    cachedItem.State = cachedItem.State switch
                    {
                        TrackState.Deleted => TrackState.Changed,
                        TrackState.NotFound => TrackState.Added,
                        _ => throw new ArgumentException($"The entry currently has state {cachedItem.State}.", nameof(key)),
                    };
                }
            }
        }

        public virtual void Commit()
        {
            lock (_lockObj)
            {
                foreach (var key in _cachedItems.Keys)
                {
                    var cachedEntry = _cachedItems[key];

                    switch (cachedEntry.State)
                    {
                        case TrackState.Added:
                            AddInternal(key, cachedEntry.Value);
                            cachedEntry.State = TrackState.None;
                            break;
                        case TrackState.Changed:
                            UpdateInternal(key, cachedEntry.Value);
                            cachedEntry.State = TrackState.None;
                            break;
                        case TrackState.Deleted:
                            DeleteInternal(key);
                            _cachedItems.Remove(key);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        // TODO: add `ClonedCache` class
        public StorageCache CloneCache() =>
            this;

        public void Delete([DisallowNull] StorageKey key)
        {
            lock (_lockObj)
            {
                if (_cachedItems.TryGetValue(key, out var cachedEntry))
                    cachedEntry.State = TrackState.Deleted;
                else
                {
                    if (ContainsInternal(key) == false) return;
                    _cachedItems.Add(key, new(key, null, TrackState.Deleted));
                }
            }
        }

        public IEnumerable<(StorageKey Key, StorageItem Value)> Seek(
            [AllowNull] byte[] keyOrPrefix = null,
            SeekDirection seekDirection = SeekDirection.Forward)
        {
            keyOrPrefix ??= [];

            if (seekDirection == SeekDirection.Backward && keyOrPrefix.Length == 0) yield break;

            var comparer = seekDirection == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            IEnumerable<KeyValuePair<StorageKey, CacheEntry>> validCacheItems;

            lock (_lockObj)
            {
                validCacheItems = _cachedItems
                    .Where(w =>
                        w.Value.State != TrackState.Deleted &&
                        w.Value.State != TrackState.NotFound);
            }

            if (keyOrPrefix.Length > 0)
                validCacheItems = validCacheItems
                    .Where(w => comparer.Compare(w.Key.ToArray(), keyOrPrefix) >= 0);

            validCacheItems = validCacheItems
                .OrderBy(o => o.Key.ToArray(), comparer);

            using var cacheIter = validCacheItems
                .Select(s => (s.Key, s.Value.Value))
                .GetEnumerator();

            using var storeIter = SeekInternal(keyOrPrefix, seekDirection)
                .GetEnumerator();

            var cachedIterMoved = cacheIter.MoveNext();
            var storeIterMoved = storeIter.MoveNext();

            while (cachedIterMoved && storeIterMoved)
            {
                var (cachedKey, cachedEntry) = cacheIter.Current;
                var (storeKey, storeEntry) = storeIter.Current;
                var compare = comparer.Compare(cachedKey.ToArray(), storeKey.ToArray());

                if (compare <= 0)
                {
                    yield return new(cachedKey, cachedEntry);
                    cachedIterMoved = cacheIter.MoveNext();
                }
                else
                {
                    yield return new(storeKey, storeEntry);
                    storeIterMoved = storeIter.MoveNext();
                }
            }

            if (cachedIterMoved | storeIterMoved)
            {
                var tailIter = cachedIterMoved ? cacheIter : storeIter;

                yield return tailIter.Current;

                while (tailIter.MoveNext())
                    yield return tailIter.Current;
            }
        }
    }
}
