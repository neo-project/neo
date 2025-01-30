// Copyright (C) 2015-2025 The Neo Project.
//
// IStorageEntry.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.IO.Caching
{
    /// <summary>
    /// Represents an entry in the <see cref="IStorageCache{TKey, TValue}"/> implementation.
    /// When Disposed, is committed to the cache.
    /// </summary>
    internal interface IStorageEntry<TKey, TValue> : IDisposable
        where TKey : class, IKeySerializable
        where TValue : class, ISerializable, new()
    {
        /// <summary>
        /// Gets the key of the storage entry.
        /// </summary>
        TKey Key { get; }

        /// <summary>
        /// Gets or set the value of the storage entry.
        /// </summary>
        TValue Value { get; set; }

        /// <summary>
        /// Gets or sets an absolute expiration date for the cache entry.
        /// </summary>
        DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets an absolute expiration time, relative to now.
        /// </summary>
        TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
    }
}
