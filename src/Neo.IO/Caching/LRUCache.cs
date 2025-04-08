// Copyright (C) 2015-2025 The Neo Project.
//
// LRUCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

namespace Neo.IO.Caching
{
    public abstract class LRUCache<TKey, TValue>(int maxCapacity, IEqualityComparer<TKey>? comparer = null)
        : Cache<TKey, TValue>(maxCapacity, comparer) where TKey : notnull
    {
        protected override void OnAccess(CacheItem item)
        {
            item.Unlink();
            Head.Add(item);
        }
    }
}
