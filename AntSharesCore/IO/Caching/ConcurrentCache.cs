using System.Collections.Generic;

namespace AntShares.IO.Caching
{
    internal abstract class ConcurrentCache<TKey, TValue> : Cache<TKey, TValue>
    {
        public override TValue this[TKey key]
        {
            get
            {
                lock (SyncRoot)
                {
                    return base[key];
                }
            }
        }

        public override int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return base.Count;
                }
            }
        }

        public object SyncRoot { get; private set; }

        protected ConcurrentCache(int max_capacity)
            : base(max_capacity)
        {
            this.SyncRoot = new object();
        }

        public override void Add(TValue item)
        {
            lock (SyncRoot)
            {
                base.Add(item);
            }
        }

        public override void Clear()
        {
            lock (SyncRoot)
            {
                base.Clear();
            }
        }

        public override bool Contains(TKey key)
        {
            lock (SyncRoot)
            {
                return base.Contains(key);
            }
        }

        public override void CopyTo(TValue[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                base.CopyTo(array, arrayIndex);
            }
        }

        public override IEnumerator<TValue> GetEnumerator()
        {
            lock (SyncRoot)
            {
                using (IEnumerator<TValue> enumerator = base.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        yield return enumerator.Current;
                    }
                }
            }
        }

        public override bool Remove(TKey key)
        {
            lock (SyncRoot)
            {
                return base.Remove(key);
            }
        }
    }
}
