// Copyright (C) 2016-2023 The Neo Project.
//
// The neo-vm is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
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
        private class TItem
        {
            public readonly TKey Key;
            public TValue Value;

            public TItem(TKey key, TValue value)
            {
                this.Key = key;
                this.Value = value;
            }
        }

        private class InternalCollection : KeyedCollection<TKey, TItem>
        {
            protected override TKey GetKeyForItem(TItem item)
            {
                return item.Key;
            }
        }

        private readonly InternalCollection collection = new();

        public int Count => collection.Count;
        public bool IsReadOnly => false;
        public ICollection<TKey> Keys => collection.Select(p => p.Key).ToArray();
        public ICollection<TValue> Values => collection.Select(p => p.Value).ToArray();

        public TValue this[TKey key]
        {
            get
            {
                return collection[key].Value;
            }
            set
            {
                if (collection.TryGetValue(key, out var entry))
                    entry.Value = value;
                else
                    Add(key, value);
            }
        }

        public void Add(TKey key, TValue value)
        {
            collection.Add(new TItem(key, value));
        }

        public bool ContainsKey(TKey key)
        {
            return collection.Contains(key);
        }

        public bool Remove(TKey key)
        {
            return collection.Remove(key);
        }

        // supress warning of value parameter nullability mismatch
#pragma warning disable CS8767
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767
        {
            if (collection.TryGetValue(key, out var entry))
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
            collection.Clear();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return collection.Contains(item.Key);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            for (int i = 0; i < collection.Count; i++)
                array[i + arrayIndex] = new KeyValuePair<TKey, TValue>(collection[i].Key, collection[i].Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return collection.Remove(item.Key);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return collection.Select(p => new KeyValuePair<TKey, TValue>(p.Key, p.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return collection.Select(p => new KeyValuePair<TKey, TValue>(p.Key, p.Value)).GetEnumerator();
        }
    }
}
