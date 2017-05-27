using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.IO.Caching
{
    public abstract class DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable
        where TValue : class, ISerializable, new()
    {
        protected internal class Trackable
        {
            public TKey Key;
            public TValue Item;
            public TrackState State;
        }

        private Dictionary<TKey, Trackable> dictionary = new Dictionary<TKey, Trackable>();

        public TValue this[TKey key]
        {
            get
            {
                Trackable trackable;
                if (dictionary.ContainsKey(key))
                {
                    trackable = dictionary[key];
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

        public void Add(TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key) && dictionary[key].State != TrackState.Deleted)
                throw new ArgumentException();
            dictionary[key] = new Trackable
            {
                Key = key,
                Item = value,
                State = TrackState.Added
            };
        }

        public void Delete(TKey key)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key].State = TrackState.Deleted;
            else
                dictionary.Add(key, new Trackable
                {
                    Key = key,
                    Item = null,
                    State = TrackState.Deleted
                });
        }

        public void DeleteWhere(Func<TKey, TValue, bool> predicate)
        {
            foreach (Trackable trackable in dictionary.Where(p => p.Value.State != TrackState.Deleted && predicate(p.Key, p.Value.Item)).Select(p => p.Value))
                trackable.State = TrackState.Deleted;
        }

        protected internal IEnumerable<Trackable> GetChangeSet()
        {
            return dictionary.Values.Where(p => p.State != TrackState.None);
        }

        protected abstract TValue GetInternal(TKey key);

        public TValue GetAndChange(TKey key, Func<TValue> factory = null)
        {
            Trackable trackable;
            if (dictionary.ContainsKey(key))
            {
                trackable = dictionary[key];
                if (trackable.State == TrackState.Deleted)
                {
                    if (factory == null) throw new KeyNotFoundException();
                    trackable.Item = factory();
                    trackable.State = TrackState.Added;
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

        public TValue GetOrAdd(TKey key, Func<TValue> factory)
        {
            Trackable trackable;
            if (dictionary.ContainsKey(key))
            {
                trackable = dictionary[key];
                if (trackable.State == TrackState.Deleted)
                {
                    trackable.Item = factory();
                    trackable.State = TrackState.Added;
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

        public TValue TryGet(TKey key)
        {
            if (dictionary.ContainsKey(key))
            {
                Trackable trackable = dictionary[key];
                if (trackable.State == TrackState.Deleted) return null;
                return trackable.Item;
            }
            else
            {
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
    }
}
