using System;
using System.Collections.Generic;

namespace Neo.IO.Caching;

abstract class KeyedCollectionSlim<TKey, TItem> where TKey : notnull
{
    private readonly LinkedList<TItem> _items = new();
    private readonly Dictionary<TKey, LinkedListNode<TItem>> dict = new();

    public int Count => dict.Count;
    public TItem First => _items.First.Value;

    protected abstract TKey GetKeyForItem(TItem item);

    public void Add(TItem item)
    {
        var key = GetKeyForItem(item);
        var node = _items.AddLast(item);
        if (!dict.TryAdd(key, node))
        {
            _items.RemoveLast();
            throw new ArgumentException("An element with the same key already exists in the collection.");
        }
    }

    public bool Contains(TKey key) => dict.ContainsKey(key);

    public void Remove(TKey key)
    {
        if (dict.Remove(key, out var node))
            _items.Remove(node);
    }

    public void RemoveFirst()
    {
        var key = GetKeyForItem(_items.First.Value);
        dict.Remove(key);
        _items.RemoveFirst();
    }
}
