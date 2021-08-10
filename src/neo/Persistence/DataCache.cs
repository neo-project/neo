using Neo.IO;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Neo.Cryptography;

namespace Neo.Persistence
{
    /// <summary>
    /// Represents a cache for the underlying storage of the NEO blockchain.
    /// </summary>
    public abstract class DataCache
    {
        /// <summary>
        /// Represents an entry in the cache.
        /// </summary>
        public class Trackable
        {
            /// <summary>
            /// The key of the entry.
            /// </summary>
            public StorageKey Key;

            /// <summary>
            /// The data of the entry.
            /// </summary>
            public StorageItem Item;

            /// <summary>
            /// The state of the entry.
            /// </summary>
            public TrackState State;

            public byte[] GetHash()
            {
                using MemoryStream ms = new();
                using BinaryWriter writer = new(ms);
                writer.Write(Key);
                writer.Write(Item);
                writer.Write((byte)State);
                return ms.ToArray().Sha256();
            }
        }

        private readonly Dictionary<StorageKey, Trackable> dictionary = new();
        private readonly HashSet<StorageKey> changeSet = new();

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
                lock (dictionary)
                {
                    if (dictionary.TryGetValue(key, out Trackable trackable))
                    {
                        if (trackable.State == TrackState.Deleted)
                            throw new KeyNotFoundException();
                    }
                    else
                    {
                        trackable = new Trackable
                        {
                            Key = key,
                            Item = GetInternal(key),
                            State = TrackState.None
                        };
                        dictionary.Add(key, trackable);
                    }
                    return trackable.Item;
                }
            }
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
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out Trackable trackable) && trackable.State != TrackState.Deleted)
                    throw new ArgumentException();
                dictionary[key] = new Trackable
                {
                    Key = key,
                    Item = value,
                    State = trackable == null ? TrackState.Added : TrackState.Changed
                };
                changeSet.Add(key);
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
        public virtual byte[] Commit()
        {
            using var sha256 = SHA256.Create();
            byte[] state = null;
            LinkedList<StorageKey> deletedItem = new();
            foreach (Trackable trackable in GetChangeSet())
            {
                state = sha256.ComputeHash(trackable.GetHash()).XOR(state);
                switch (trackable.State)
                {
                    case TrackState.Added:
                        AddInternal(trackable.Key, trackable.Item);
                        trackable.State = TrackState.None;
                        break;
                    case TrackState.Changed:
                        UpdateInternal(trackable.Key, trackable.Item);
                        trackable.State = TrackState.None;
                        break;
                    case TrackState.Deleted:
                        DeleteInternal(trackable.Key);
                        deletedItem.AddFirst(trackable.Key);
                        break;
                }
            }

            foreach (StorageKey key in deletedItem)
            {
                dictionary.Remove(key);
            }
            changeSet.Clear();
            return state;
        }

        /// <summary>
        /// Creates a snapshot, which uses this instance as the underlying storage.
        /// </summary>
        /// <returns>The snapshot of this instance.</returns>
        public DataCache CreateSnapshot()
        {
            return new ClonedCache(this);
        }

        /// <summary>
        /// Deletes an entry from the cache.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        public void Delete(StorageKey key)
        {
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out Trackable trackable))
                {
                    if (trackable.State == TrackState.Added)
                    {
                        dictionary.Remove(key);
                        changeSet.Remove(key);
                    }
                    else
                    {
                        trackable.State = TrackState.Deleted;
                        changeSet.Add(key);
                    }
                }
                else
                {
                    StorageItem item = TryGetInternal(key);
                    if (item == null) return;
                    dictionary.Add(key, new Trackable
                    {
                        Key = key,
                        Item = item,
                        State = TrackState.Deleted
                    });
                    changeSet.Add(key);
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
        /// <param name="key_prefix">The prefix of the key.</param>
        /// <returns>The entries found with the desired prefix.</returns>
        public IEnumerable<(StorageKey Key, StorageItem Value)> Find(byte[] key_prefix = null)
        {
            foreach (var (key, value) in Seek(key_prefix, SeekDirection.Forward))
                if (key.ToArray().AsSpan().StartsWith(key_prefix))
                    yield return (key, value);
                else
                    yield break;
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
            ByteArrayComparer comparer = direction == SeekDirection.Forward
                ? ByteArrayComparer.Default
                : ByteArrayComparer.Reverse;
            foreach (var (key, value) in Seek(start, direction))
                if (comparer.Compare(key.ToArray(), end) < 0)
                    yield return (key, value);
                else
                    yield break;
        }

        /// <summary>
        /// Gets the change set in the cache.
        /// </summary>
        /// <returns>The change set.</returns>
        public IEnumerable<Trackable> GetChangeSet()
        {
            lock (dictionary)
            {
                foreach (StorageKey key in changeSet)
                    yield return dictionary[key];
            }
        }

        /// <summary>
        /// Determines whether the cache contains the specified entry.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <returns><see langword="true"/> if the cache contains an entry with the specified key; otherwise, <see langword="false"/>.</returns>
        public bool Contains(StorageKey key)
        {
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out Trackable trackable))
                {
                    if (trackable.State == TrackState.Deleted) return false;
                    return true;
                }
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
        /// <returns>The data of the entry. Or <see langword="null"/> if the entry doesn't exist.</returns>
        protected abstract StorageItem GetInternal(StorageKey key);

        /// <summary>
        /// Reads a specified entry from the cache, and mark it as <see cref="TrackState.Changed"/>. If the entry is not in the cache, it will be automatically loaded from the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="factory">A delegate used to create the entry if it doesn't exist. If the entry already exists, the factory will not be used.</param>
        /// <returns>The cached data. Or <see langword="null"/> if it doesn't exist and the <paramref name="factory"/> is not provided.</returns>
        public StorageItem GetAndChange(StorageKey key, Func<StorageItem> factory = null)
        {
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out Trackable trackable))
                {
                    if (trackable.State == TrackState.Deleted)
                    {
                        if (factory == null) return null;
                        trackable.Item = factory();
                        trackable.State = TrackState.Changed;
                    }
                    else if (trackable.State == TrackState.None)
                    {
                        trackable.State = TrackState.Changed;
                        changeSet.Add(key);
                    }
                }
                else
                {
                    trackable = new Trackable
                    {
                        Key = key,
                        Item = TryGetInternal(key)
                    };
                    if (trackable.Item == null)
                    {
                        if (factory == null) return null;
                        trackable.Item = factory();
                        trackable.State = TrackState.Added;
                    }
                    else
                    {
                        trackable.State = TrackState.Changed;
                    }
                    dictionary.Add(key, trackable);
                    changeSet.Add(key);
                }
                return trackable.Item;
            }
        }

        /// <summary>
        /// Reads a specified entry from the cache. If the entry is not in the cache, it will be automatically loaded from the underlying storage. If the entry doesn't exist, the factory will be used to create a new one.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="factory">A delegate used to create the entry if it doesn't exist. If the entry already exists, the factory will not be used.</param>
        /// <returns>The cached data.</returns>
        public StorageItem GetOrAdd(StorageKey key, Func<StorageItem> factory)
        {
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out Trackable trackable))
                {
                    if (trackable.State == TrackState.Deleted)
                    {
                        trackable.Item = factory();
                        trackable.State = TrackState.Changed;
                    }
                }
                else
                {
                    trackable = new Trackable
                    {
                        Key = key,
                        Item = TryGetInternal(key)
                    };
                    if (trackable.Item == null)
                    {
                        trackable.Item = factory();
                        trackable.State = TrackState.Added;
                        changeSet.Add(key);
                    }
                    else
                    {
                        trackable.State = TrackState.None;
                    }
                    dictionary.Add(key, trackable);
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
        public IEnumerable<(StorageKey Key, StorageItem Value)> Seek(byte[] keyOrPrefix = null, SeekDirection direction = SeekDirection.Forward)
        {
            IEnumerable<(byte[], StorageKey, StorageItem)> cached;
            HashSet<StorageKey> cachedKeySet;
            ByteArrayComparer comparer = direction == SeekDirection.Forward ? ByteArrayComparer.Default : ByteArrayComparer.Reverse;
            lock (dictionary)
            {
                cached = dictionary
                    .Where(p => p.Value.State != TrackState.Deleted && (keyOrPrefix == null || comparer.Compare(p.Key.ToArray(), keyOrPrefix) >= 0))
                    .Select(p =>
                    (
                        KeyBytes: p.Key.ToArray(),
                        p.Key,
                        p.Value.Item
                    ))
                    .OrderBy(p => p.KeyBytes, comparer)
                    .ToArray();
                cachedKeySet = new HashSet<StorageKey>(dictionary.Keys);
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
            bool c1 = e1.MoveNext();
            bool c2 = e2.MoveNext();
            i1 = c1 ? e1.Current : default;
            i2 = c2 ? e2.Current : default;
            while (c1 || c2)
            {
                if (!c2 || (c1 && comparer.Compare(i1.KeyBytes, i2.KeyBytes) < 0))
                {
                    yield return (i1.Key, i1.Item);
                    c1 = e1.MoveNext();
                    i1 = c1 ? e1.Current : default;
                }
                else
                {
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
        public StorageItem TryGet(StorageKey key)
        {
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out Trackable trackable))
                {
                    if (trackable.State == TrackState.Deleted) return null;
                    return trackable.Item;
                }
                StorageItem value = TryGetInternal(key);
                if (value == null) return null;
                dictionary.Add(key, new Trackable
                {
                    Key = key,
                    Item = value,
                    State = TrackState.None
                });
                return value;
            }
        }

        /// <summary>
        /// Reads a specified entry from the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <returns>The data of the entry. Or <see langword="null"/> if it doesn't exist.</returns>
        protected abstract StorageItem TryGetInternal(StorageKey key);

        /// <summary>
        /// Updates an entry in the underlying storage.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        protected abstract void UpdateInternal(StorageKey key, StorageItem value);
    }
}
