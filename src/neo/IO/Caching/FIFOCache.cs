// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IO.Caching
{
    internal abstract class FIFOCache<TKey, TValue> : Cache<TKey, TValue>
    {
        public FIFOCache(int max_capacity)
            : base(max_capacity)
        {
        }

        protected override void OnAccess(CacheItem item)
        {
        }
    }
}
