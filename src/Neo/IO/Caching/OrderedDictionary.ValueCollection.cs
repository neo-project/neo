using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Caching;

partial class OrderedDictionary<TKey, TValue>
{
    class ValueCollection : ICollection<TValue>, IReadOnlyList<TValue>
    {
        private readonly InternalCollection internalCollection;

        public ValueCollection(InternalCollection internalCollection)
        {
            this.internalCollection = internalCollection;
        }

        public TValue this[int index] => internalCollection[index].Value;

        public int Count => internalCollection.Count;

        public bool IsReadOnly => true;

        public void Add(TValue item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(TValue item) => internalCollection.Any(p => p.Value.Equals(item));

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            for (int i = 0; i < internalCollection.Count && i + arrayIndex < array.Length; i++)
                array[i + arrayIndex] = internalCollection[i].Value;
        }

        public IEnumerator<TValue> GetEnumerator() => internalCollection.Select(p => p.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Remove(TValue item) => throw new NotSupportedException();
    }
}
