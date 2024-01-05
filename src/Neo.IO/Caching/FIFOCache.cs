// Copyright (C) 2015-2024 The Neo Project.
//
// FIFOCache.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.IO.Caching
{
    internal abstract class FIFOCache<TKey, TValue> : Cache<TKey, TValue>
    {
        public FIFOCache(int max_capacity, IEqualityComparer<TKey> comparer = null)
            : base(max_capacity, comparer)
        {
        }

        protected override void OnAccess(CacheItem item)
        {
        }
    }
}
