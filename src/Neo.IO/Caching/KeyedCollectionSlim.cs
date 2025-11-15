// Copyright (C) 2015-2025 The Neo Project.
//
// KeyedCollectionSlim.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections;
using System.Runtime.CompilerServices;

namespace Neo.IO.Caching;

/// <summary>
/// A slimmed down version of KeyedCollection;
/// </summary>
/// <typeparam name="TKey">The type of the keys.</typeparam>
/// <typeparam name="TItem">The type of the items.</typeparam>
internal abstract class KeyedCollectionSlim<TKey, TItem> : IReadOnlyCollection<TItem> where TKey : notnull
{
    private LinkedList<TItem> _items = new();

    private readonly Dictionary<TKey, LinkedListNode<TItem>> _dict;

    /// <summary>
    /// Gets the number of items in the collection.
    /// </summary>
    public int Count => _dict.Count;

    /// <summary>
    /// Gets the first item in the collection, or the default value if the collection is empty.
    /// </summary>
    public TItem? FirstOrDefault => _items.First is not null ? _items.First.Value : default;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedCollectionSlim{TKey, TItem}"/> class.
    /// </summary>
    /// <param name="initialcapacity">The initial capacity of the collection.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialcapacity"/> is less than 0.</exception>
    public KeyedCollectionSlim(int initialcapacity = 0)
    {
        if (initialcapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialcapacity), $"{initialcapacity} less than 0.");
        _dict = new(initialcapacity);
    }

    /// <summary>
    /// Gets the key for an item.
    /// </summary>
    /// <param name="item">The item to get the key for.</param>
    /// <returns>The key for the item.</returns>
    protected abstract TKey GetKeyForItem(TItem item);

    /// <summary>
    /// Adds an item to the collection.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns><see langword="true"/> if the item was added; otherwise, <see langword="false"/>.</returns>
    public bool TryAdd(TItem item)
    {
        var key = GetKeyForItem(item);
        var node = _items.AddLast(item);
        if (!_dict.TryAdd(key, node))
        {
            _items.RemoveLast();
            return false;
        }
        return true;
    }

    /// <summary>
    /// Determines whether the collection contains an item with the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the collection.</param>
    /// <returns>
    /// <see langword="true"/> if the collection contains an item with the key; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TKey key) => _dict.ContainsKey(key);

    /// <summary>
    /// Removes an item from the collection.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the item was removed successfully; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TKey key)
    {
        if (_dict.Remove(key, out var node))
        {
            _items.Remove(node);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes the first item from the collection.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the item was removed successfully; otherwise, <see langword="false"/>.
    /// </returns>
    public bool RemoveFirst()
    {
        var first = _items.First;
        if (first is null) return false;

        var key = GetKeyForItem(first.Value);
        _dict.Remove(key);
        _items.RemoveFirst();
        return true;
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        // reset _items, not _items.Clear(), because LinkedList.Clear() is O(n).
        _items = new();
        _dict.Clear();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<TItem> GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
