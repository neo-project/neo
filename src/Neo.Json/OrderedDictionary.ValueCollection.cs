// Copyright (C) 2015-2023 The Neo Project.
// 
// The Neo.Json is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections;

namespace Neo.Json;

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

        public bool Contains(TValue item) => item is null ? internalCollection.Any(p => p is null) : internalCollection.Any(p => item.Equals(p.Value));

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
