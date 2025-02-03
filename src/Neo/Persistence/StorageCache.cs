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
        private readonly Dictionary<StorageKey, CacheEntry> _cachedItems = new(StorageKeyEqualityComparer.Instance);
        private readonly HashSet<StorageKey> _changeSet = [];

        private readonly object _lockObj = new();

        #region IReadOnlyStoreView

        /// <inheritdoc />
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
                            cachedEntry = new(key, storeEntry, TrackState.None);
                            _cachedItems.Add(key, cachedEntry);
                        }
                    }

                    return cachedEntry.Value;
                }
            }
        }

        /// <inheritdoc />
        public bool TryGet([DisallowNull] StorageKey key, [NotNullWhen(true)] out StorageItem value)
        {
            value = null;

            lock (_lockObj)
            {
                if (_cachedItems.TryGetValue(key, out var cacheEntry))
                {
                    if (cacheEntry.State == TrackState.Deleted || cacheEntry.State == TrackState.NotFound)
                        return false;
                    value = cacheEntry.Value;
                    return true;
                }

                var storeEntry = TryGetInternal(key);

                if (storeEntry is null) return false;

                value = storeEntry;
                _cachedItems.Add(key, new(key, storeEntry, TrackState.None));

                return true;
            }
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
                        _ => throw new ArgumentException($"The entry currently has a state of {cachedItem.State}.", nameof(key)),
                    };
                }
                _changeSet.Add(key);
            }
        }

        public virtual void Commit()
        {
            lock (_lockObj)
            {
                foreach (var key in _changeSet)
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

                _changeSet.Clear();
            }
        }

        [Obsolete("CreateSnapshot is deprecated, please use CloneCache instead.")]
        public StorageCache CreateSnapshot() =>
            new ClonedCache(this);

        public StorageCache CloneCache() =>
            new ClonedCache(this);

        public void Delete([DisallowNull] StorageKey key)
        {
            lock (_lockObj)
            {
                if (_cachedItems.TryGetValue(key, out var cachedEntry))
                {
                    if (cachedEntry.State == TrackState.Added)
                    {
                        cachedEntry.State = TrackState.NotFound;
                        _changeSet.Remove(key);
                    }
                    else if (cachedEntry.State != TrackState.NotFound)
                    {
                        cachedEntry.State = TrackState.Deleted;
                        _changeSet.Add(key);
                    }
                }
                else
                {
                    var storeEntry = TryGetInternal(key);

                    if (storeEntry is null) return;
                    _cachedItems.Add(key, new(key, storeEntry, TrackState.Deleted));
                    _changeSet.Add(key);
                }
            }
        }

        public IEnumerable<(StorageKey Key, StorageItem Value)> Seek(
            [AllowNull] byte[] keyOrPrefix = null,
            SeekDirection seekDirection = SeekDirection.Forward)
        {
            if (seekDirection == SeekDirection.Backward && (keyOrPrefix is null || keyOrPrefix.Length == 0)) yield break;

            var comparer = seekDirection == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            IEnumerable<KeyValuePair<StorageKey, CacheEntry>> validCacheItems;

            lock (_lockObj)
            {
                validCacheItems = _cachedItems
                    .Where(w =>
                        w.Value.State != TrackState.Deleted &&
                        w.Value.State != TrackState.NotFound);

                if (keyOrPrefix?.Length > 0)
                    validCacheItems = validCacheItems
                        .Where(w => comparer.Compare(w.Key.ToArray(), keyOrPrefix) >= 0);

                validCacheItems = validCacheItems
                    .OrderBy(o => o.Key.ToArray(), comparer);
            }

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
                    yield return new(cachedKey.ToArray(), cachedEntry.Clone());
                    cachedIterMoved = cacheIter.MoveNext();
                }
                else
                {
                    yield return new(storeKey.ToArray(), storeEntry.Clone());
                    storeIterMoved = storeIter.MoveNext();
                }
            }

            if (cachedIterMoved | storeIterMoved)
            {
                var tailIter = cachedIterMoved ? cacheIter : storeIter;

                yield return tailIter.Current;

                while (tailIter.MoveNext())
                {
                    var (key, value) = tailIter.Current;
                    yield return new(key.ToArray(), value.Clone());
                }
            }
        }

        public IEnumerable<(StorageKey Key, StorageItem Value)> Find(
            [AllowNull] byte[] keyOrPrefix = null,
            SeekDirection seekDirection = SeekDirection.Forward)
        {
            if (seekDirection == SeekDirection.Backward && (keyOrPrefix is null || keyOrPrefix.Length == 0)) yield break;

            var lastKey = new byte[ApplicationEngine.MaxStorageKeySize];
            Array.Fill<byte>(lastKey, 0xff);

            keyOrPrefix?.CopyTo(lastKey, 0);

            var results = seekDirection == SeekDirection.Backward ?
                FindRange(lastKey, keyOrPrefix, seekDirection) :
                FindRange(keyOrPrefix, lastKey, seekDirection);

            foreach (var (key, value) in results)
                yield return new(key, value);
        }

        public IEnumerable<(StorageKey Key, StorageItem Value)> FindRange(
            [DisallowNull] byte[] startKeyOrPrefix,
            [DisallowNull] byte[] lastKeyOrPrefix,
            SeekDirection seekDirection = SeekDirection.Forward)
        {
            var comparer = seekDirection == SeekDirection.Forward ?
                ByteArrayComparer.Default :
                ByteArrayComparer.Reverse;

            foreach (var (key, value) in Seek(startKeyOrPrefix, seekDirection))
            {
                if (comparer.Compare(key.ToArray(), lastKeyOrPrefix) <= 0)
                    yield return new(key, value);
                else
                    yield break;
            }
        }

        public IEnumerable<CacheEntry> GetChangeSet()
        {
            lock (_lockObj)
            {
                foreach (var key in _changeSet)
                    yield return _cachedItems[key];
            }
        }

        public bool Contains([DisallowNull] StorageKey key)
        {
            lock (_lockObj)
            {
                if (_cachedItems.TryGetValue(key, out var cacheEntry))
                    return cacheEntry.State != TrackState.Deleted && cacheEntry.State != TrackState.NotFound;
                return ContainsInternal(key);
            }
        }

        public StorageItem GetAndChange([DisallowNull] StorageKey key, [AllowNull] Func<StorageItem> getNewValue = null)
        {
            lock (_lockObj)
            {
                if (_cachedItems.TryGetValue(key, out var cacheEntry))
                {
                    if (cacheEntry.State == TrackState.Deleted || cacheEntry.State == TrackState.NotFound)
                    {
                        if (getNewValue is null) return null;
                        cacheEntry.Value = getNewValue();

                        if (cacheEntry.State == TrackState.Deleted)
                            cacheEntry.State = TrackState.Changed;
                        else
                        {
                            cacheEntry.State = TrackState.Added;
                            _changeSet.Add(key);
                        }
                    }
                    else if (cacheEntry.State == TrackState.None)
                    {
                        cacheEntry.State = TrackState.Changed;
                        _changeSet.Add(key);
                    }
                }
                else
                {
                    var storeEntry = TryGetInternal(key);

                    if (storeEntry is not null)
                        cacheEntry = new(key, storeEntry, TrackState.Changed);
                    else
                    {
                        if (getNewValue is null) return null;
                        cacheEntry = new(key, getNewValue(), TrackState.Added);
                    }
                    _cachedItems.Add(key, cacheEntry);
                    _changeSet.Add(key);
                }
                return cacheEntry.Value;
            }
        }

        [return: MaybeNull]
        public StorageItem GetOrAdd([DisallowNull] StorageKey key, [AllowNull] Func<StorageItem> addNewValue = null)
        {
            lock (_lockObj)
            {
                if (_cachedItems.TryGetValue(key, out var cacheEntry))
                {
                    if (cacheEntry.State == TrackState.Deleted || cacheEntry.State == TrackState.NotFound)
                    {
                        if (addNewValue is null)
                            return cacheEntry.Value;
                        else
                            cacheEntry.Value = addNewValue();

                        if (cacheEntry.State == TrackState.Deleted)
                            cacheEntry.State = TrackState.Changed;
                        else
                        {
                            cacheEntry.State = TrackState.Added;
                            _changeSet.Add(key);
                        }
                    }
                }
                else
                {
                    var storeEntry = TryGetInternal(key);

                    if (storeEntry is null)
                    {
                        cacheEntry = new(key, addNewValue(), TrackState.Added);
                        _changeSet.Add(key);
                    }
                    else
                        cacheEntry = new(key, storeEntry, TrackState.None);
                    _cachedItems.Add(key, cacheEntry);
                }

                return cacheEntry.Value;
            }
        }

        [return: MaybeNull]
        public StorageItem TryGet([DisallowNull] StorageKey key)
        {
            TryGet(key, out var value);
            return value;
        }
    }
}
