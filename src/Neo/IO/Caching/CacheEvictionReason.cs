// Copyright (C) 2015-2025 The Neo Project.
//
// CacheEvictionReason.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IO.Caching
{
    /// <summary>
    /// Specifies the reasons why an entry was evicted from the cache.
    /// </summary>
    public enum CacheEvictionReason : byte
    {
        /// <summary>
        /// The item was not removed from the cache.
        /// </summary>
        None = 0,

        /// <summary>
        /// The item was removed from the cache manually.
        /// </summary>
        Removed = 1,

        /// <summary>
        /// The item was removed from the cache because it was overwritten.
        /// </summary>
        Replaced = 2,

        /// <summary>
        /// The item was removed from the cache because it timed out.
        /// </summary>
        Expired = 3,

        /// <summary>
        /// The item was removed from the cache because its token expired.
        /// </summary>
        TokenExpired = 4,

        /// <summary>
        /// The item was removed from the cache because it exceeded its capacity.
        /// </summary>
        Capacity = 5,
    }
}
