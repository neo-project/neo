// Copyright (C) 2015-2025 The Neo Project.
//
// OrderedDictionary.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Neo.VM.Collections
{
    internal class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : notnull
    {
        private class TItem(TKey key, TValue value)
        {
            public readonly TKey Key = key;
            public TValue Value = value;
        }

        private class InternalCollection : KeyedCollection<TKey, TItem>
        {
            protected override TKey GetKeyForItem(TItem item) => item.Key;
        }

        private readonly InternalCollection _collection = new();

        public int Count => _collection.Count;

        public bool IsReadOnly => false;

        public ICollection<TKey> Keys => _collection.Select(p => p.Key).ToArray();

        public ICollection<TValue> Values => _collection.Select(p => p.Value).ToArray();


        public TValue this[TKey key]
        {
            get
            {
                return _collection[key].Value;
            }
            set
            {
                if (_collection.TryGetValue(key, out var entry))
                    entry.Value = value;
                else
                    Add(key, value);
            }
        }

        public void Add(TKey key, TValue value)
        {
            _collection.Add(new TItem(key, value));
        }

        public bool ContainsKey(TKey key)
        {
            return _collection.Contains(key);
        }

        public bool Remove(TKey key)
        {
            return _collection.Remove(key);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (_collection.TryGetValue(key, out var entry))
            {
                value = entry.Value;
                return true;
            }
            value = default;
            return false;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _collection.Clear();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return _collection.Contains(item.Key);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            for (int i = 0; i < _collection.Count; i++)
                array[i + arrayIndex] = new KeyValuePair<TKey, TValue>(_collection[i].Key, _collection[i].Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return _collection.Remove(item.Key);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return _collection.Select(p => new KeyValuePair<TKey, TValue>(p.Key, p.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.Select(p => new KeyValuePair<TKey, TValue>(p.Key, p.Value)).GetEnumerator();
        }
    }
}
