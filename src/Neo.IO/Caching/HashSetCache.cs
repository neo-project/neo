// Copyright (C) 2015-2025 The Neo Project.
//
// HashSetCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using System.Collections;
using System.Runtime.CompilerServices;

namespace Neo.IO.Caching;

/// <summary>
/// A cache that uses a hash set to store items.
/// </summary>
/// <typeparam name="T">The type of the items in the cache.</typeparam>
internal class HashSetCache<T> : ICollection<T> where T : IEquatable<T>
{
    private class Items(int initialCapacity) : KeyedCollectionSlim<T, T>(initialCapacity)
    {
        protected sealed override T GetKeyForItem(T item) => item;
    }

    private readonly int _capacity;
    private readonly Items _items;

    /// <summary>
    /// Gets the number of items in the cache.
    /// </summary>
    public int Count => _items.Count;

    public bool IsReadOnly => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashSetCache{T}"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of items in the cache.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
    public HashSetCache(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), $"{capacity} less than 0.");

        _capacity = capacity;
        // Avoid allocating a large memory at initialization
        _items = new(Math.Min(capacity, 4096));
    }

    /// <summary>
    /// Adds an item to the cache.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>
    /// <see langword="true"/> if the item was added; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(T item)
    {
        if (!_items.TryAdd(item)) return false;
        if (_items.Count > _capacity) _items.RemoveFirst();
        return true;
    }

    /// <summary>
    /// Determines whether the cache contains an item.
    /// </summary>
    /// <param name="item">The item to locate in the cache.</param>
    /// <returns>
    /// <see langword="true"/> if the item is found in the cache; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item) => _items.Contains(item);

    /// <summary>
    /// Removes all items from the cache.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _items.Clear();

    /// <summary>
    /// Removes an item from the cache.
    /// </summary>
    /// <param name="items">The items to remove.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ExceptWith(IEnumerable<T> items)
    {
        foreach (var item in items) _items.Remove(item);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the cache.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the cache.</returns>
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the cache.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the cache.</returns>
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public void Add(T item)
    {
        _ = TryAdd(item);
    }

    public bool Remove(T item)
    {
        return _items.Remove(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));

        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("The number of elements in the source ICollection<T> is greater than the available space from arrayIndex to the end of the destination array.");

        var i = arrayIndex;
        foreach (var item in this)
        {
            array[i++] = item;
        }
    }
}

#nullable disable
