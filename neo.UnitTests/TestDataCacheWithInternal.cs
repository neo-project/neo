using Neo.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests
{
    public class TestDataCacheWithInternal<TKey, TValue> : TestDataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private readonly Dictionary<TKey, TValue> _internal = new Dictionary<TKey, TValue>();

        public override void DeleteInternal(TKey key) => _internal.Remove(key);

        protected override void AddInternal(TKey key, TValue value) => _internal.Add(key, value);

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            return Enumerable.Empty<KeyValuePair<TKey, TValue>>();
        }

        protected override TValue GetInternal(TKey key) => _internal[key];

        protected override TValue TryGetInternal(TKey key)
        {
            _internal.TryGetValue(key, out var value);
            return value;
        }

        protected override void UpdateInternal(TKey key, TValue value) => _internal[key] = value;
    }
}