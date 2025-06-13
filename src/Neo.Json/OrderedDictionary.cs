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
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Json
{
    partial class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
    {
        private class TItem
        {
            public TKey Key { get; }
            public TValue Value { get; set; }

            public TItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private class InternalCollection : KeyedCollection<TKey, TItem>
        {
            protected override TKey GetKeyForItem(TItem item)
            {
                return item.Key;
            }
        }

        private readonly InternalCollection _collection = new();

        public int Count => _collection.Count;
        public bool IsReadOnly => false;
        public IReadOnlyList<TKey> Keys { get; }
        public IReadOnlyList<TValue> Values { get; }
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => (KeyCollection)Keys;
        ICollection<TValue> IDictionary<TKey, TValue>.Values => (ValueCollection)Values;

        public OrderedDictionary()
        {
            Keys = new KeyCollection(_collection);
            Values = new ValueCollection(_collection);
        }

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

        public TValue this[int index]
        {
            get
            {
                return _collection[index].Value;
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

#pragma warning disable CS8767
        public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
        {
            if (_collection.TryGetValue(key, out var entry))
            {
                value = entry.Value;
                return value != null;
            }
            value = default;
            return false;
        }
#pragma warning restore CS8767

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
            for (var i = 0; i < _collection.Count; i++)
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
