// Copyright (C) 2015-2022 The Neo Project.
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
