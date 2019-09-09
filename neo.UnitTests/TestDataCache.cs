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

        public TestDataCache()
        {
            _defaultValue = null;
        }

        public TestDataCache(TValue defaultValue)
        {
            this._defaultValue = defaultValue;
        }
        public override void DeleteInternal(TKey key)
        {
        }

        protected override void AddInternal(TKey key, TValue value)
        {
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            return Enumerable.Empty<KeyValuePair<TKey, TValue>>();
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
