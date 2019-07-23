using Neo.Ledger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Caching
{
    public abstract class DataCache<TKey, TValue> : IComparer<TKey>
        where TKey : IEquatable<TKey>, ISerializable
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        public class Trackable
        {
            public TKey Key;
            public TValue Item;
            public TrackState State;
        }

        private readonly Dictionary<TKey, Trackable> dictionary = new Dictionary<TKey, Trackable>();

        public TValue this[TKey key]
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

        public void Add(TKey key, TValue value)
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
            }
        }

        protected abstract void AddInternal(TKey key, TValue value);

        public void Commit()
        {
            foreach (Trackable trackable in GetChangeSet())
                switch (trackable.State)
                {
                    case TrackState.Added:
                        AddInternal(trackable.Key, trackable.Item);
                        break;
                    case TrackState.Changed:
                        UpdateInternal(trackable.Key, trackable.Item);
                        break;
                    case TrackState.Deleted:
                        DeleteInternal(trackable.Key);
                        break;
                }
        }

        public DataCache<TKey, TValue> CreateSnapshot()
        {
            return new CloneCache<TKey, TValue>(this);
        }

        public void Delete(TKey key)
        {
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out Trackable trackable))
                {
                    if (trackable.State == TrackState.Added)
                        dictionary.Remove(key);
                    else
                        trackable.State = TrackState.Deleted;
                }
                else
                {
                    TValue item = TryGetInternal(key);
                    if (item == null) return;
                    dictionary.Add(key, new Trackable
                    {
                        Key = key,
                        Item = item,
                        State = TrackState.Deleted
                    });
                }
            }
        }

        public abstract void DeleteInternal(TKey key);

        public void DeleteWhere(Func<TKey, TValue, bool> predicate)
        {
            lock (dictionary)
            {
                foreach (Trackable trackable in dictionary.Where(p => p.Value.State != TrackState.Deleted && predicate(p.Key, p.Value.Item)).Select(p => p.Value))
                    trackable.State = TrackState.Deleted;
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> Find(byte[] key_prefix = null)
        {
            lock (dictionary)
            {
                // Retrive cached results

                var cached = dictionary.
                    Where(pair => pair.Value.State != TrackState.Deleted &&
                    (
                        key_prefix == null || pair.Key.ToArray()
                        .Take(key_prefix.Length)
                        .SequenceEqual(key_prefix))
                    )
                    .Select(u => new KeyValuePair<TKey, TValue>(u.Key, u.Value.Item))
                    .ToList();

                var withCache = cached.Count == 0;
                KeyValuePair<TKey, TValue> n = new KeyValuePair<TKey, TValue>();

                using (var iterator = FindInternal(key_prefix ?? new byte[0]).GetEnumerator())
                {
                    do
                    {
                        KeyValuePair<TKey, TValue> a = n, b = n;

                        if (iterator.MoveNext())
                        {
                            a = iterator.Current;
                            if (iterator.MoveNext()) b = iterator.Current;
                        }

                        // We need to return a, b and cached results between them

                        if (a.Key == null && a.Value == null)
                        {
                            // Without internal data

                            foreach (var item in cached) yield return item;
                            yield break;
                        }
                        else if (b.Key == null && b.Value == null)
                        {
                            // The last internal data, sort cached with them

                            foreach
                                (
                                var item in cached
                                .Concat(new KeyValuePair<TKey, TValue>[] { a }).OrderBy(u => u.Key, this)
                                .ToArray()
                                )
                            {
                                yield return item;
                            }

                            // We don't need to process anything else.
                            break;
                        }
                        else
                        {
                            if (withCache)
                            {
                                yield return a;
                                yield return b;
                            }
                            else
                            {
                                // We need to find the item between 'a' and 'b'

                                foreach
                                    (
                                    var item in cached
                                    .Concat(new KeyValuePair<TKey, TValue>[] { a, b })
                                    .OrderBy(u => u.Key, this)
                                    .ToArray()
                                    )
                                {
                                    yield return item;

                                    if (Compare(item.Key, b.Key) == 0) break;

                                    // Remove it from cache

                                    cached.Remove(item);
                                }

                                withCache = cached.Count == 0;
                            }
                        }
                    }
                    while (true);
                }
            }
        }

        public int Compare(TKey x, TKey y)
        {
            if (x is StorageKey xx && y is StorageKey yy)
            {
                var result = xx.ScriptHash.CompareTo(yy.ScriptHash);
                if (result != 0) return result;

                return Compare(xx.Key, yy.Key);
            }
            else
            {
                var a = x.ToArray();
                var b = y.ToArray();

                return Compare(a, b);
            }
        }

        private int Compare(byte[] a, byte[] b)
        {
            for (int index = 0, max = Math.Min(a.Length, b.Length); index < max; index++)
            {
                var result = a[index].CompareTo(b[index]);
                if (result != 0) return result;
            }
            return a.Length.CompareTo(b.Length);
        }

        protected abstract IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix);

        public IEnumerable<Trackable> GetChangeSet()
        {
            lock (dictionary)
            {
                foreach (Trackable trackable in dictionary.Values.Where(p => p.State != TrackState.None))
                    yield return trackable;
            }
        }

        protected abstract TValue GetInternal(TKey key);

        public TValue GetAndChange(TKey key, Func<TValue> factory = null)
        {
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out Trackable trackable))
                {
                    if (trackable.State == TrackState.Deleted)
                    {
                        if (factory == null) throw new KeyNotFoundException();
                        trackable.Item = factory();
                        trackable.State = TrackState.Changed;
                    }
                    else if (trackable.State == TrackState.None)
                    {
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
                        if (factory == null) throw new KeyNotFoundException();
                        trackable.Item = factory();
                        trackable.State = TrackState.Added;
                    }
                    else
                    {
                        trackable.State = TrackState.Changed;
                    }
                    dictionary.Add(key, trackable);
                }
                return trackable.Item;
            }
        }

        public TValue GetOrAdd(TKey key, Func<TValue> factory)
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

        public TValue TryGet(TKey key)
        {
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out Trackable trackable))
                {
                    if (trackable.State == TrackState.Deleted) return null;
                    return trackable.Item;
                }
                TValue value = TryGetInternal(key);
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

        protected abstract TValue TryGetInternal(TKey key);

        protected abstract void UpdateInternal(TKey key, TValue value);
    }
}
