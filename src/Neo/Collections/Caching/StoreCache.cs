// Copyright (C) 2015-2025 The Neo Project.
//
// StoreCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Persistence;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Neo.Collections.Caching
{
    public class StoreCache<TKey, TValue>(IStore store) : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>, ICollection, IDictionary
        where TKey : class, IKeySerializable
        where TValue : class, ISerializable, new()
    {
        private static readonly ConcurrentDictionary<TKey, WeakReference<TValue>> s_memoryCache = new(Math.Min(Environment.ProcessorCount, 16), 0, KeyValueSerializableEqualityComparer<TKey>.Default);

        private readonly IStore _store = store ?? throw new ArgumentNullException(nameof(store));

        /// <inheritdoc />
        public TValue this[TKey key]
        {
            get
            {
                if (TryGetSync(key, out var value) == false)
                    throw new KeyNotFoundException();

                return value;
            }
            set
            {
                TryAddSync(key, value);
            }
        }

        /// <inheritdoc />
        public object this[object key]
        {
            get
            {
                if (TryGetSync(key as TKey, out var value) == false)
                    throw new KeyNotFoundException();

                return value;
            }
            set
            {
                TryAddSync(key as TKey, value as TValue);
            }
        }

        /// <inheritdoc />
        public int Count => s_memoryCache.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public bool IsFixedSize => false;

        /// <inheritdoc />
        public bool IsSynchronized => false;

        /// <inheritdoc />
        public object SyncRoot => throw new NotSupportedException();

        /// <inheritdoc />
        public ICollection<TKey> Keys => s_memoryCache.Keys;

        /// <inheritdoc />
        ICollection IDictionary.Keys => ((IDictionary)s_memoryCache).Keys;

        /// <inheritdoc />
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => s_memoryCache.Keys;

        /// <inheritdoc />
        public ICollection<TValue> Values => s_memoryCache.Values.Select(GetTargetValue).ToArray();

        /// <inheritdoc />
        ICollection IDictionary.Values => s_memoryCache.Values.Select(GetTargetValue).ToArray();

        /// <inheritdoc />
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => s_memoryCache.Values.Select(GetTargetValue);

        /// <inheritdoc />
        public void Add(TKey key, TValue value)
        {
            TryAddSync(key, value);
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            TryAddSync(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Add(object key, object value)
        {
            if (key is TKey tKey && value is TValue tValue)
                TryAddSync(tKey, tValue);
        }

        /// <inheritdoc />
        public void Clear()
        {
            s_memoryCache.Clear();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return s_memoryCache.ContainsKey(item.Key) || _store.Contains(item.Key.ToArray());
        }

        /// <inheritdoc />
        public bool Contains(object key)
        {
            if (key is TKey tKey)
                return s_memoryCache.ContainsKey(tKey) || _store.Contains(tKey.ToArray()); ;
            return false;
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            return s_memoryCache.ContainsKey(key) || _store.Contains(key.ToArray());
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public bool Remove(TKey key)
        {
            return TryRemoveSync(key, out _);
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return TryRemoveSync(item.Key, out _);
        }

        /// <inheritdoc />
        public void Remove(object key)
        {
            if (key is TKey tKey)
                TryRemoveSync(tKey, out _);
        }

        /// <inheritdoc />
        public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue value)
        {
            return TryGetSync(key, out value);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return s_memoryCache.Select(s => new KeyValuePair<TKey, TValue>(s.Key, GetTargetValue(s.Value))).GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return s_memoryCache.ToDictionary(key => key.Key, value => GetTargetValue(value.Value)).GetEnumerator();
        }

        private static TValue GetTargetValue(WeakReference<TValue> valueRef)
        {
            if (valueRef.TryGetTarget(out var value))
                return value;

            throw new NullReferenceException();
        }

        private bool TryRemoveSync(TKey key, [NotNullWhen(true)] out TValue value)
        {
            if (s_memoryCache.TryRemove(key, out var valueRef))
            {
                _store.Delete(key.ToArray());

                return valueRef.TryGetTarget(out value);
            }
            else
            {
                if (_store.TryGet(key.ToArray(), out var rawValue))
                {
                    value = rawValue.AsSerializable<TValue>();

                    _store.Delete(key.ToArray());

                    return true;
                }

                value = default;
                return false;
            }
        }

        private bool TryGetSync(TKey key, [NotNullWhen(true)] out TValue value)
        {
            if (s_memoryCache.TryGetValue(key, out var valueRef))
            {
                if (valueRef.TryGetTarget(out value) == false && _store.TryGet(key.ToArray(), out var rawValue))
                    value = rawValue.AsSerializable<TValue>();
                return true;
            }
            else
            {
                if (_store.TryGet(key.ToArray(), out var rawValue))
                {
                    // We do want to catch exceptions for serializer.
                    // We want exceptions to be thrown. There is no
                    // fast way to check "rawValue" bytes to see if
                    // data is "TValue" type or "typeof(TValue)".
                    //
                    // NOTE:
                    //      Another cache class or IStore can overwrite
                    //      "rawValue" bytes in the Store. Making it NOT
                    //      "typeof(TValue)" for a given key.
                    value = rawValue.AsSerializable<TValue>();

                    return s_memoryCache.TryAdd(key, new(value, false));
                }
            }

            value = default;
            return false;
        }

        private bool TryAddSync(TKey key, TValue value)
        {
            if (s_memoryCache.TryGetValue(key, out var valueRef))
            {
                if (valueRef.TryGetTarget(out var oldValue) && ReferenceEquals(value, oldValue) == false)
                {
                    // Update target for cache
                    valueRef.SetTarget(value);

                    // Save to store
                    //
                    // NOTE:
                    //      This method of sync can change the
                    //      "value" serializable type. If two
                    //      caching classes use the same key
                    //      but different ISerializable classes
                    _store.Put(key.ToArray(), value.ToArray());
                }

                return true;
            }
            else
            {
                if (s_memoryCache.TryAdd(key, new(value, false)))
                {
                    // Save to store
                    //
                    // NOTE:
                    //      This method of sync can change the
                    //      "value" serializable type. If two
                    //      caching classes use the same key
                    //      but different ISerializable classes
                    _store.Put(key.ToArray(), value.ToArray());

                    return true;
                }
            }

            return false;
        }
    }
}
