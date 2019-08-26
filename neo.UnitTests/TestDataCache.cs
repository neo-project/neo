using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.UnitTests
{
    public class TestDataCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private readonly TValue _defaultValue;
        private readonly TKey _defaultKey;

        public TestDataCache()
        {
            _defaultValue = null;
            _defaultKey = default(TKey);
        }

        public TestDataCache(TValue defaultValue)
        {
            this._defaultValue = defaultValue;
        }

        public TestDataCache(TKey key, TValue value)
        {
            this._defaultValue = value;
            this._defaultKey = key;
        }

        public override void DeleteInternal(TKey key)
        {
        }

        protected override void AddInternal(TKey key, TValue value)
        {
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            if (_defaultValue is null)
            {
                return Enumerable.Empty<KeyValuePair<TKey, TValue>>();
            }
            var list = new List<KeyValuePair<TKey, TValue>>
            {
                new KeyValuePair<TKey, TValue>(_defaultKey, _defaultValue)
            };
            return list;
        }

        protected override TValue GetInternal(TKey key)
        {
            if (_defaultValue == null) throw new NotImplementedException();
            return _defaultValue;
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return _defaultValue;
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
        }
    }
}
