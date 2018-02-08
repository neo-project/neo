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
            throw new NotImplementedException();
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return null;
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
        }
    }
}
