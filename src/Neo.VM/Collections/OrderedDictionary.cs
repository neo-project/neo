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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

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
        private KeyCollection? _keys;
        private ValueCollection? _values;

        public int Count => _collection.Count;

        public bool IsReadOnly => false;

        public ICollection<TKey> Keys => _keys ??= new KeyCollection(_collection);

        public ICollection<TValue> Values => _values ??= new ValueCollection(_collection);


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
            return new Enumerator(_collection);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(_collection);
        }

        private sealed class KeyCollection : ICollection<TKey>
        {
            private readonly InternalCollection _collection;

            public KeyCollection(InternalCollection collection)
            {
                _collection = collection;
            }

            public int Count => _collection.Count;

            public bool IsReadOnly => true;

            public void Add(TKey item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Contains(TKey item) => _collection.Contains(item);

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                if (array is null)
                    throw new ArgumentNullException(nameof(array));

                for (int i = 0; i < _collection.Count; i++)
                    array[arrayIndex + i] = _collection[i].Key;
            }

            public bool Remove(TKey item) => throw new NotSupportedException();

            public IEnumerator<TKey> GetEnumerator() => new KeyEnumerator(_collection);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ValueCollection : ICollection<TValue>
        {
            private readonly InternalCollection _collection;

            public ValueCollection(InternalCollection collection)
            {
                _collection = collection;
            }

            public int Count => _collection.Count;

            public bool IsReadOnly => true;

            public void Add(TValue item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Contains(TValue item)
            {
                var comparer = EqualityComparer<TValue>.Default;
                for (int i = 0; i < _collection.Count; i++)
                {
                    if (comparer.Equals(_collection[i].Value, item))
                        return true;
                }
                return false;
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (array is null)
                    throw new ArgumentNullException(nameof(array));

                for (int i = 0; i < _collection.Count; i++)
                    array[arrayIndex + i] = _collection[i].Value;
            }

            public bool Remove(TValue item) => throw new NotSupportedException();

            public IEnumerator<TValue> GetEnumerator() => new ValueEnumerator(_collection);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly InternalCollection _collection;
            private int _index;

            internal Enumerator(InternalCollection collection)
            {
                _collection = collection;
                _index = -1;
                Current = default;
            }

            public KeyValuePair<TKey, TValue> Current { get; private set; }

            readonly object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (++_index >= _collection.Count)
                    return false;

                var item = _collection[_index];
                Current = new KeyValuePair<TKey, TValue>(item.Key, item.Value);
                return true;
            }

            public void Reset()
            {
                _index = -1;
                Current = default;
            }

            public readonly void Dispose()
            {
            }
        }

        private struct KeyEnumerator : IEnumerator<TKey>
        {
            private readonly InternalCollection _collection;
            private int _index;

            internal KeyEnumerator(InternalCollection collection)
            {
                _collection = collection;
                _index = -1;
                Current = default!;
            }

            public TKey Current { get; private set; }

            readonly object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                if (++_index >= _collection.Count)
                    return false;

                Current = _collection[_index].Key;
                return true;
            }

            public void Reset()
            {
                _index = -1;
                Current = default!;
            }

            public readonly void Dispose()
            {
            }
        }

        private struct ValueEnumerator : IEnumerator<TValue>
        {
            private readonly InternalCollection _collection;
            private int _index;

            internal ValueEnumerator(InternalCollection collection)
            {
                _collection = collection;
                _index = -1;
                Current = default!;
            }

            public TValue Current { get; private set; }

            readonly object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                if (++_index >= _collection.Count)
                    return false;

                Current = _collection[_index].Value;
                return true;
            }

            public void Reset()
            {
                _index = -1;
                Current = default!;
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
