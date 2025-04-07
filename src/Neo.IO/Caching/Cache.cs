// Copyright (C) 2015-2025 The Neo Project.
//
// Cache.cs file belongs to the neo project and is free
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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Neo.IO.Caching
{
    public abstract class Cache<TKey, TValue>(int maxCapacity, IEqualityComparer<TKey>? comparer = null)
        : ICollection<TValue>, IDisposable where TKey : notnull
    {
        protected class CacheItem
        {
            public readonly TKey Key;
            public readonly TValue Value;

            private CacheItem _prev, _next;

            public CacheItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
                _prev = this;
                _next = this;
            }

            public bool IsEmpty => ReferenceEquals(_prev, this);

            /// <summary>
            /// Adds an item after the current item.
            /// </summary>
            /// <param name="another">The item to add.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(CacheItem another) => another.Link(this, _next);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Link(CacheItem prev, CacheItem next)
            {
                _prev = prev;
                _next = next;
                prev._next = this;
                next._prev = this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Unlink()
            {
                _prev._next = _next;
                _next._prev = _prev;
                _prev = this;
                _next = this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public CacheItem? RemovePrevious()
            {
                if (IsEmpty) return null;
                var prev = _prev;
                prev.Unlink();
                return prev;
            }
        }

        // key-value in head is invalid, and it is used as a sentinel.
        protected readonly CacheItem _head = new(default!, default!);

#if NET9_0_OR_GREATER
        protected readonly Lock _lock = new();
#else
        protected readonly object _lock = new();
#endif

        protected readonly Dictionary<TKey, CacheItem> _innerDictionary = new(comparer);

        public TValue this[TKey key]
        {
            get
            {
                lock (_lock)
                {
                    if (!_innerDictionary.TryGetValue(key, out var item))
                        throw new KeyNotFoundException();
                    OnAccess(item);
                    return item.Value;
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _innerDictionary.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public bool IsDisposable { get; } = typeof(IDisposable).IsAssignableFrom(typeof(TValue));

        public void Add(TValue item)
        {
            var key = GetKeyForItem(item);
            lock (_lock)
            {
                AddInternal(key, item);
            }
        }

        private void AddInternal(TKey key, TValue item)
        {
            if (_innerDictionary.TryGetValue(key, out var cached))
            {
                OnAccess(cached);
            }
            else
            {
                if (_innerDictionary.Count >= maxCapacity)
                {
                    var prev = _head.RemovePrevious();
                    if (prev is not null) RemoveInternal(prev.Key);
                }

                var added = new CacheItem(key, item);
                _innerDictionary.Add(key, added);
                _head.Add(added);
            }
        }

        public void AddRange(IEnumerable<TValue> items)
        {
            lock (_lock)
            {
                foreach (var item in items)
                {
                    var key = GetKeyForItem(item);
                    AddInternal(key, item);
                }
            }
        }

        public void Clear()
        {
            CacheItem[]? items = null;

            lock (_lock)
            {
                if (IsDisposable)
                {
                    items = [.. _innerDictionary.Values];
                }
                _innerDictionary.Clear();
                _head.Unlink();
            }

            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item.Value is IDisposable disposable) disposable.Dispose();
                }
            }
        }

        public bool Contains(TKey key)
        {
            lock (_lock)
            {
                if (!_innerDictionary.TryGetValue(key, out var cached)) return false;
                OnAccess(cached);
                return true;
            }
        }

        public bool Contains(TValue item)
        {
            return Contains(GetKeyForItem(item));
        }

        public void CopyTo(TValue[] array, int startIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));

            lock (_lock)
            {
                var count = _innerDictionary.Count;
                if (startIndex + count > array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(startIndex),
                        $"startIndex({startIndex}) + Count({count}) > Length({array.Length})");
                }

                foreach (var item in _innerDictionary.Values)
                {
                    array[startIndex++] = item.Value;
                }
            }
        }

        public void Dispose()
        {
            Clear();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            TValue[] values;
            lock (_lock)
            {
                var index = 0;
                values = new TValue[_innerDictionary.Count];
                foreach (var item in _innerDictionary.Values)
                {
                    values[index++] = item.Value;
                }
            }
            return values.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected abstract TKey GetKeyForItem(TValue item);

        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                return RemoveInternal(key);
            }
        }

        protected abstract void OnAccess(CacheItem item);

        public bool Remove(TValue item)
        {
            return Remove(GetKeyForItem(item));
        }

        private bool RemoveInternal(TKey key)
        {
            if (!_innerDictionary.Remove(key, out var item)) return false;

            item.Unlink();
            if (IsDisposable && item.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
            return true;
        }

        public bool TryGet(TKey key, out TValue item)
        {
            lock (_lock)
            {
                if (_innerDictionary.TryGetValue(key, out var cached))
                {
                    OnAccess(cached);
                    item = cached.Value;
                    return true;
                }
            }
            item = default!;
            return false;
        }
    }
}
