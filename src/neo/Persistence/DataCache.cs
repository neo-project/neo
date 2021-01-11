using Neo.IO;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Persistence
{
    public abstract class DataCache
    {
        public class Trackable
        {
            public StorageKey Key;
            public StorageItem Item;
            public TrackState State;
        }

        private readonly Dictionary<StorageKey, Trackable> dictionary = new Dictionary<StorageKey, Trackable>();
        private readonly HashSet<StorageKey> changeSet = new HashSet<StorageKey>();

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
        /// Try to Add a specific key, with associated value, to the current cached dictionary.
        /// It will not read from internal state.
        /// However, if previously cached into Dictionary, request may fail.
        /// </summary>
        /// <param name="key">Key to be possible added.
        /// Key will not be added if value exists cached and the modification was not a Deleted one.
        /// </param>
        /// <param name="value">Corresponding value to be added, in the case of sucess.</param>
        /// <exception cref="ArgumentException">If cached on dictionary, with any state rather than `Deleted`, an Exception will be raised.</exception>
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

        protected abstract void AddInternal(StorageKey key, StorageItem value);

        /// <summary>
        /// Update internals with all changes cached on Dictionary which are not None.
        /// </summary>
        public void Commit()
        {
            LinkedList<StorageKey> deletedItem = new LinkedList<StorageKey>();
            foreach (Trackable trackable in GetChangeSet())
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
            foreach (StorageKey key in deletedItem)
            {
                dictionary.Remove(key);
            }
            changeSet.Clear();
        }

        public DataCache CreateSnapshot()
        {
            return new ClonedCache(this);
        }

        /// <summary>
        /// Delete key from cached Dictionary or search in Internal.
        /// </summary>
        /// <param name="key">Key to be deleted.</param>
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

        protected abstract void DeleteInternal(StorageKey key);

        /// <summary>
        /// Find the entries that start with the `key_prefix`
        /// </summary>
        /// <param name="key_prefix">Must maintain the deserialized format of StorageKey</param>
        /// <returns>Entries found with the desired prefix</returns>
        public IEnumerable<(StorageKey Key, StorageItem Value)> Find(byte[] key_prefix = null)
        {
            foreach (var (key, value) in Seek(key_prefix, SeekDirection.Forward))
                if (key.ToArray().AsSpan().StartsWith(key_prefix))
                    yield return (key, value);
                else
                    yield break;
        }

        /// <summary>
        /// Find the entries that between [start, end)
        /// </summary>
        /// <param name="start">Start key (inclusive)</param>
        /// <param name="end">End key (exclusive)</param>
        /// <param name="direction">The search direction.</param>
        /// <returns>Entries found with the desired range</returns>
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

        public IEnumerable<Trackable> GetChangeSet()
        {
            lock (dictionary)
            {
                foreach (StorageKey key in changeSet)
                    yield return dictionary[key];
            }
        }

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

        protected abstract bool ContainsInternal(StorageKey key);

        protected abstract StorageItem GetInternal(StorageKey key);

        /// <summary>
        /// Try to Get a specific key from current cached dictionary.
        /// Otherwise, tries to get from internal data with TryGetInternal.
        /// </summary>
        /// <param name="key">Key to be searched.</param>
        /// <param name="factory">Function that may replace current object stored. 
        /// If object already exists the factory passed as parameter will not be used.
        /// </param>
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
        /// Seek to the entry with specific key
        /// </summary>
        /// <param name="keyOrPrefix">The key to be sought</param>
        /// <param name="direction">The direction of seek</param>
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
            using (var e1 = cached.GetEnumerator())
            using (var e2 = uncached.GetEnumerator())
            {
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
        }

        protected abstract IEnumerable<(StorageKey Key, StorageItem Value)> SeekInternal(byte[] keyOrPrefix, SeekDirection direction);

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

        protected abstract StorageItem TryGetInternal(StorageKey key);

        protected abstract void UpdateInternal(StorageKey key, StorageItem value);
    }
}
