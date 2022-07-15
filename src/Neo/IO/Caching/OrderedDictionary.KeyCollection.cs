using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Caching;

partial class OrderedDictionary<TKey, TValue>
{
    class KeyCollection : ICollection<TKey>, IReadOnlyList<TKey>
    {
        private readonly InternalCollection internalCollection;

        public KeyCollection(InternalCollection internalCollection)
        {
            this.internalCollection = internalCollection;
        }

        public TKey this[int index] => internalCollection[index].Key;

        public int Count => internalCollection.Count;

        public bool IsReadOnly => true;

        public void Add(TKey item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(TKey item) => internalCollection.Contains(item);

        public void CopyTo(TKey[] array, int arrayIndex)
        {
            for (int i = 0; i < internalCollection.Count && i + arrayIndex < array.Length; i++)
                array[i + arrayIndex] = internalCollection[i].Key;
        }

        public IEnumerator<TKey> GetEnumerator() => internalCollection.Select(p => p.Key).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Remove(TKey item) => throw new NotSupportedException();
    }
}
