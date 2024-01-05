// Copyright (C) 2015-2024 The Neo Project.
//
// KeyedCollectionSlim.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.IO.Caching;

abstract class KeyedCollectionSlim<TKey, TItem> where TKey : notnull
{
    private readonly LinkedList<TItem> _items = new();
    private readonly Dictionary<TKey, LinkedListNode<TItem>> _dict = new();

    public int Count => _dict.Count;
    public TItem First => _items.First.Value;

    protected abstract TKey GetKeyForItem(TItem item);

    public void Add(TItem item)
    {
        var key = GetKeyForItem(item);
        var node = _items.AddLast(item);
        if (!_dict.TryAdd(key, node))
        {
            _items.RemoveLast();
            throw new ArgumentException("An element with the same key already exists in the collection.");
        }
    }

    public bool Contains(TKey key) => _dict.ContainsKey(key);

    public void Remove(TKey key)
    {
        if (_dict.Remove(key, out var node))
            _items.Remove(node);
    }

    public void RemoveFirst()
    {
        var key = GetKeyForItem(_items.First.Value);
        _dict.Remove(key);
        _items.RemoveFirst();
    }
}
