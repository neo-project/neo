using System;

namespace AntShares.IO.Caching
{
    internal class CacheItem<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;
        public DateTime LastUpdate;

        public CacheItem(TKey key, TValue value)
        {
            this.Key = key;
            this.Value = value;
        }

        public void Update()
        {
            LastUpdate = DateTime.Now;
        }
    }
}
