// Copyright (C) 2015-2025 The Neo Project.
//
// DataCache.cs file belongs to the neo project and is free
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Persistence
{
    /// <summary>
    /// Represents a cache for the underlying storage of the NEO blockchain.
    /// </summary>
    public abstract class DataCache : IReadOnlyStore
    {
        /// <summary>
        /// Represents an entry in the cache.
        /// </summary>
        [DebuggerDisplay("{Item.ToString()}, State = {State.ToString()}")]
        public class Trackable(StorageItem item, TrackState state)
        {
            /// <summary>
            /// The data of the entry.
            /// </summary>
            public StorageItem Item { get; set; } = item;

            /// <summary>
            /// The state of the entry.
            /// </summary>
            public TrackState State { get; set; } = state;
        }

        /// <summary>
        /// Delegate for storage entries
        /// </summary>
        /// <param name="sender">DataCache</param>
        /// <param name="key">Key</param>
        /// <param name="item">Item</param>
        public delegate void OnEntryDelegate(DataCache sender, StorageKey key, StorageItem item);

        private readonly Dictionary<StorageKey, Trackable> _dictionary = [];
        private readonly HashSet<StorageKey>? _changeSet;

        public event OnEntryDelegate? OnRead;
        public event OnEntryDelegate? OnUpdate;

        /// <summary>
        /// True if DataCache is readOnly
        /// </summary>
        public bool IsReadOnly => _changeSet == null;

        /// <summary>
        /// Reads a specified entry from the cache. If the entry is not in the cache, it will be automatically loaded from the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <returns>The cached data.</returns>
        /// <exception cref="KeyNotFoundException">If the entry doesn't exist.</exception>
        public StorageItem this[StorageKey key]
        {
            get
            {
                lock (_dictionary)
                {
                    if (_dictionary.TryGetValue(key, out var trackable))
                    {
                        if (trackable.State == TrackState.Deleted || trackable.State == TrackState.NotFound)
                            throw new KeyNotFoundException();
                    }
                    else
                    {
                        trackable = new Trackable(GetInternal(key), TrackState.None);
                        _dictionary.Add(key, trackable);
                    }
                    return trackable.Item;
                }
            }
        }

        /// <summary>
        /// Data cache constructor
        /// </summary>
        /// <param name="readOnly">True if you don't want to allow writes</param>
        protected DataCache(bool readOnly)
        {
            if (!readOnly)
                _changeSet = [];
        }

        /// <summary>
        /// Adds a new entry to the cache.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        /// <exception cref="ArgumentException">The entry has already been cached.</exception>
        /// <remarks>Note: This method does not read the internal storage to check whether the record already exists.</remarks>
        public void Add(StorageKey key, StorageItem value)
        {
            lock (_dictionary)
            {
                if (_dictionary.TryGetValue(key, out var trackable))
                {
                    trackable.Item = value;
                    trackable.State = trackable.State switch
                    {
                        TrackState.Deleted => TrackState.Changed,
                        TrackState.NotFound => TrackState.Added,
                        _ => throw new ArgumentException($"The element currently has state {trackable.State}")
                    };
                }
                else
                {
                    _dictionary[key] = new Trackable(value, TrackState.Added);
                }
                _changeSet?.Add(key);
            }
        }

        /// <summary>
        /// Adds a new entry to the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        protected abstract void AddInternal(StorageKey key, StorageItem value);

        /// <summary>
        /// Commits all changes in the cache to the underlying storage.
        /// </summary>
        public virtual void Commit()
        {
            if (_changeSet is null)
            {
                throw new InvalidOperationException("DataCache is read only");
            }

            lock (_dictionary)
            {
                foreach (var key in _changeSet)
                {
                    var trackable = _dictionary[key];
                    switch (trackable.State)
                    {
                        case TrackState.Added:
                            AddInternal(key, trackable.Item);
                            trackable.State = TrackState.None;
                            break;
                        case TrackState.Changed:
                            UpdateInternalWrapper(key, trackable.Item);
                            trackable.State = TrackState.None;
                            break;
                        case TrackState.Deleted:
                            DeleteInternal(key);
                            _dictionary.Remove(key);
                            break;
                    }
                }
                _changeSet.Clear();
            }
        }

        /// <summary>
        /// Gets the change set in the cache.
        /// </summary>
        /// <returns>The change set.</returns>
        public IEnumerable<KeyValuePair<StorageKey, Trackable>> GetChangeSet()
        {
            if (_changeSet is null)
            {
                throw new InvalidOperationException("DataCache is read only");
            }

            lock (_dictionary)
            {
                foreach (var key in _changeSet)
                    yield return new(key, _dictionary[key]);
            }
        }

        /// <summary>
        /// Creates a snapshot, which uses this instance as the underlying storage.
        /// </summary>
        /// <returns>The snapshot of this instance.</returns>
        [Obsolete("CreateSnapshot is deprecated, please use CloneCache instead.")]
        public DataCache CreateSnapshot()
        {
            return new ClonedCache(this);
        }

        /// <summary>
        /// Creates a clone of the snapshot cache, which uses this instance as the underlying storage.
        /// </summary>
        /// <returns>The <see cref="ClonedCache"/> of this <see cref="DataCache"/> instance.</returns>
        public DataCache CloneCache()
        {
            return new ClonedCache(this);
        }

        /// <summary>
        /// Deletes an entry from the cache.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        public void Delete(StorageKey key)
        {
            lock (_dictionary)
            {
                if (_dictionary.TryGetValue(key, out var trackable))
                {
                    if (trackable.State == TrackState.Added)
                    {
                        trackable.State = TrackState.NotFound;
                        _changeSet?.Remove(key);
                    }
                    else if (trackable.State != TrackState.NotFound)
                    {
                        trackable.State = TrackState.Deleted;
                        _changeSet?.Add(key);
                    }
                }
                else
                {
                    var item = TryGetInternalWrapper(key);
                    if (item == null) return;
                    _dictionary.Add(key, new Trackable(item, TrackState.Deleted));
                    _changeSet?.Add(key);
                }
            }
        }

        /// <summary>
        /// Deletes an entry from the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        protected abstract void DeleteInternal(StorageKey key);

        /// <summary>
        /// Finds the entries starting with the specified prefix.
        /// </summary>
        /// <param name="direction">The search direction.</param>
        /// <returns>The entries found with the desired prefix.</returns>
        public IEnumerable<(StorageKey Key, StorageItem Value)> Find(SeekDirection direction = SeekDirection.Forward)
        {
            return Find((byte[]?)null, direction);
        }

        /// <inheritdoc/>
        public IEnumerable<(StorageKey Key, StorageItem Value)> Find(StorageKey? keyPrefix = null, SeekDirection direction = SeekDirection.Forward)
        {
            var key = keyPrefix?.ToArray();
            return Find(key, direction);
        }

        /// <summary>
        /// Finds the entries starting with the specified prefix.
        /// </summary>
        /// <param name="keyPrefix">The prefix of the key.</param>
        /// <param name="direction">The search direction.</param>
        /// <returns>The entries found with the desired prefix.</returns>
        public IEnumerable<(StorageKey Key, StorageItem Value)> Find(byte[]? keyPrefix = null, SeekDirection direction = SeekDirection.Forward)
        {
            var seekPrefix = keyPrefix;
            if (direction == SeekDirection.Backward)
            {
                ArgumentNullException.ThrowIfNull(keyPrefix);
                if (keyPrefix.Length == 0)
                {
                    // Backwards seek for zero prefix is not supported for now.
                    throw new ArgumentOutOfRangeException(nameof(keyPrefix));
                }
                seekPrefix = null;
                for (var i = keyPrefix.Length - 1; i >= 0; i--)
                {
                    if (keyPrefix[i] < 0xff)
                    {
                        seekPrefix = keyPrefix.Take(i + 1).ToArray();
                        // The next key after the keyPrefix.
                        seekPrefix[i]++;
                        break;
                    }
                }
                if (seekPrefix == null)
                {
                    throw new ArgumentException($"{nameof(keyPrefix)} with all bytes being 0xff is not supported now");
                }
            }
            return FindInternal(keyPrefix, seekPrefix, direction);
        }

        private IEnumerable<(StorageKey Key, StorageItem Value)> FindInternal(byte[]? keyPrefix, byte[]? seekPrefix, SeekDirection direction)
        {
            foreach (var (key, value) in Seek(seekPrefix, direction))
            {
                if (keyPrefix == null || key.ToArray().AsSpan().StartsWith(keyPrefix))
                    yield return (key, value);
                else if (direction == SeekDirection.Forward || (seekPrefix == null || !key.ToArray().SequenceEqual(seekPrefix)))
                    yield break;
            }
        }

        /// <summary>
        /// Finds the entries that between [start, end).
        /// </summary>
        /// <param name="start">The start key (inclusive).</param>
        /// <param name="end">The end key (exclusive).</param>
        /// <param name="direction">The search direction.</param>
        /// <returns>The entries found with the desired range.</returns>
        public IEnumerable<(StorageKey Key, StorageItem Value)> FindRange(byte[] start, byte[] end, SeekDirection direction = SeekDirection.Forward)
        {
            var comparer = direction == SeekDirection.Forward
                ? ByteArrayComparer.Default
                : ByteArrayComparer.Reverse;
            foreach (var (key, value) in Seek(start, direction))
            {
                if (comparer.Compare(key.ToArray(), end) < 0)
                    yield return (key, value);
                else
                    yield break;
            }
        }

        /// <summary>
        /// Determines whether the cache contains the specified entry.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <returns><see langword="true"/> if the cache contains an entry with the specified key; otherwise, <see langword="false"/>.</returns>
        public bool Contains(StorageKey key)
        {
            lock (_dictionary)
            {
                if (_dictionary.TryGetValue(key, out var trackable))
                    return trackable.State != TrackState.Deleted && trackable.State != TrackState.NotFound;
                return ContainsInternal(key);
            }
        }

        /// <summary>
        /// Determines whether the underlying storage contains the specified entry.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <returns><see langword="true"/> if the underlying storage contains an entry with the specified key; otherwise, <see langword="false"/>.</returns>
        protected abstract bool ContainsInternal(StorageKey key);

        /// <summary>
        /// Reads a specified entry from the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <returns>The data of the entry. Or throw <see cref="KeyNotFoundException"/> if the entry doesn't exist.</returns>
        /// <exception cref="KeyNotFoundException">If the entry doesn't exist.</exception>
        protected abstract StorageItem GetInternal(StorageKey key);

        /// <summary>
        /// Reads a specified entry from the cache, and mark it as <see cref="TrackState.Changed"/>.
        /// If the entry is not in the cache, it will be automatically loaded from the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="factory">
        /// A delegate used to create the entry if it doesn't exist.
        /// If the entry already exists, the factory will not be used.
        /// </param>
        /// <returns>
        /// The cached data, or <see langword="null"/> if it doesn't exist and the <paramref name="factory"/> is not provided.
        /// </returns>
        [return: NotNullIfNotNull(nameof(factory))]
        public StorageItem? GetAndChange(StorageKey key, Func<StorageItem>? factory = null)
        {
            lock (_dictionary)
            {
                if (_dictionary.TryGetValue(key, out var trackable))
                {
                    if (trackable.State == TrackState.Deleted || trackable.State == TrackState.NotFound)
                    {
                        if (factory == null) return null;
                        trackable.Item = factory();
                        if (trackable.State == TrackState.Deleted)
                        {
                            trackable.State = TrackState.Changed;
                        }
                        else
                        {
                            trackable.State = TrackState.Added;
                            _changeSet?.Add(key);
                        }
                    }
                    else if (trackable.State == TrackState.None)
                    {
                        trackable.State = TrackState.Changed;
                        _changeSet?.Add(key);
                    }
                }
                else
                {
                    var item = TryGetInternalWrapper(key);
                    if (item == null)
                    {
                        if (factory == null) return null;
                        trackable = new Trackable(factory(), TrackState.Added);
                    }
                    else
                    {
                        trackable = new Trackable(item, TrackState.Changed);
                    }
                    _dictionary.Add(key, trackable);
                    _changeSet?.Add(key);
                }
                return trackable.Item;
            }
        }

        private StorageItem? TryGetInternalWrapper(StorageKey key)
        {
            var item = TryGetInternal(key);
            if (item == null) return null;

            OnRead?.Invoke(this, key, item);
            return item;
        }

        private void UpdateInternalWrapper(StorageKey key, StorageItem value)
        {
            UpdateInternal(key, value);
            OnUpdate?.Invoke(this, key, value);
        }

        /// <summary>
        /// Reads a specified entry from the cache.
        /// If the entry is not in the cache, it will be automatically loaded from the underlying storage.
        /// If the entry doesn't exist, the factory will be used to create a new one.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="factory">
        /// A delegate used to create the entry if it doesn't exist.
        /// If the entry already exists, the factory will not be used.
        /// </param>
        /// <returns>The cached data.</returns>
        public StorageItem GetOrAdd(StorageKey key, Func<StorageItem> factory)
        {
            lock (_dictionary)
            {
                if (_dictionary.TryGetValue(key, out var trackable))
                {
                    if (trackable.State == TrackState.Deleted || trackable.State == TrackState.NotFound)
                    {
                        trackable.Item = factory();
                        if (trackable.State == TrackState.Deleted)
                        {
                            trackable.State = TrackState.Changed;
                        }
                        else
                        {
                            trackable.State = TrackState.Added;
                            _changeSet?.Add(key);
                        }
                    }
                }
                else
                {
                    var item = TryGetInternalWrapper(key);
                    if (item == null)
                    {
                        trackable = new Trackable(factory(), TrackState.Added);
                        _changeSet?.Add(key);
                    }
                    else
                    {
                        trackable = new Trackable(item, TrackState.None);
                    }
                    _dictionary.Add(key, trackable);
                }
                return trackable.Item;
            }
        }

        /// <summary>
        /// Seeks to the entry with the specified key.
        /// </summary>
        /// <param name="keyOrPrefix">The key to be sought.</param>
        /// <param name="direction">The direction of seek.</param>
        /// <returns>An enumerator containing all the entries after seeking.</returns>
        public IEnumerable<(StorageKey Key, StorageItem Value)> Seek(byte[]? keyOrPrefix = null, SeekDirection direction = SeekDirection.Forward)
        {
            IEnumerable<(byte[], StorageKey, StorageItem)> cached;
            HashSet<StorageKey> cachedKeySet;
            var comparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            lock (_dictionary)
            {
                cached = _dictionary
                    .Where(p => p.Value.State != TrackState.Deleted && p.Value.State != TrackState.NotFound && (keyOrPrefix == null || comparer.Compare(p.Key.ToArray(), keyOrPrefix) >= 0))
                    .Select(p =>
                    (
                        KeyBytes: p.Key.ToArray(),
                        p.Key,
                        p.Value.Item
                    ))
                    .OrderBy(p => p.KeyBytes, comparer)
                    .ToArray();
                cachedKeySet = new HashSet<StorageKey>(_dictionary.Keys);
            }
            var uncached = SeekInternal(keyOrPrefix ?? Array.Empty<byte>(), direction)
                .Where(p => !cachedKeySet.Contains(p.Key))
                .Select(p =>
                (
                    KeyBytes: p.Key.ToArray(),
                    p.Key,
                    p.Value
                ));
            using var e1 = cached.GetEnumerator();
            using var e2 = uncached.GetEnumerator();
            (byte[] KeyBytes, StorageKey Key, StorageItem Item) i1, i2;
            var c1 = e1.MoveNext();
            var c2 = e2.MoveNext();
            i1 = c1 ? e1.Current : default;
            i2 = c2 ? e2.Current : default;
            while (c1 || c2)
            {
                if (!c2 || (c1 && comparer.Compare(i1.KeyBytes, i2.KeyBytes) < 0))
                {
                    if (i1.Key == null || i1.Item == null) throw new NullReferenceException("SeekInternal returned a null key or item");
                    yield return (i1.Key, i1.Item);
                    c1 = e1.MoveNext();
                    i1 = c1 ? e1.Current : default;
                }
                else
                {
                    if (i2.Key == null || i2.Item == null) throw new NullReferenceException("SeekInternal returned a null key or item");
                    yield return (i2.Key, i2.Item);
                    c2 = e2.MoveNext();
                    i2 = c2 ? e2.Current : default;
                }
            }
        }

        /// <summary>
        /// Seeks to the entry with the specified key in the underlying storage.
        /// </summary>
        /// <param name="keyOrPrefix">The key to be sought.</param>
        /// <param name="direction">The direction of seek.</param>
        /// <returns>An enumerator containing all the entries after seeking.</returns>
        protected abstract IEnumerable<(StorageKey Key, StorageItem Value)> SeekInternal(byte[] keyOrPrefix, SeekDirection direction);

        /// <summary>
        /// Reads a specified entry from the cache. If the entry is not in the cache, it will be automatically loaded from the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <returns>The cached data. Or <see langword="null"/> if it is neither in the cache nor in the underlying storage.</returns>
        public StorageItem? TryGet(StorageKey key)
        {
            lock (_dictionary)
            {
                if (_dictionary.TryGetValue(key, out var trackable))
                {
                    if (trackable.State == TrackState.Deleted || trackable.State == TrackState.NotFound)
                        return null;
                    return trackable.Item;
                }
                var value = TryGetInternalWrapper(key);
                if (value == null) return null;
                _dictionary.Add(key, new Trackable(value, TrackState.None));
                return value;
            }
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(StorageKey key, [NotNullWhen(true)] out StorageItem? item)
        {
            item = TryGet(key);
            return item != null;
        }

        /// <summary>
        /// Reads a specified entry from the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <returns>The data of the entry. Or <see langword="null"/> if it doesn't exist.</returns>
        protected abstract StorageItem? TryGetInternal(StorageKey key);

        /// <summary>
        /// Updates an entry in the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        protected abstract void UpdateInternal(StorageKey key, StorageItem value);
    }
}
