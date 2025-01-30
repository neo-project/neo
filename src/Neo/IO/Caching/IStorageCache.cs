// Copyright (C) 2015-2025 The Neo Project.
//
// IStorageCache.cs file belongs to the neo project and is free
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
    /// Represents a local storage cache whose values are not serialized.
    /// </summary>
    internal interface IStorageCache<TKey, TValue> : IDisposable
        where TKey : class, IKeySerializable
        where TValue : class, ISerializable, new()
    {
        /// <summary>
        /// Gets the item associated with this key if present.
        /// </summary>
        /// <param name="key">An <see cref="IKeySerializable"/> object identifying the requested entry.</param>
        /// <param name="value">The located value or null.</param>
        /// <returns><see langword="true" /> if the key was found.</returns>
        bool TryGetValue(TKey key, out TValue value);

        /// <summary>
        /// Create an entry in the cache.
        /// </summary>
        /// <param name="key">An <see cref="IKeySerializable"/> object identifying the entry.</param>
        /// <param name="value">The <see cref="ISerializable"/> object you want to cache.</param>
        void AddOrUpdate(TKey key, TValue value);

        /// <summary>
        /// Removes the object associated with the given key.
        /// </summary>
        /// <param name="key">An <see cref="IKeySerializable"/> object identifying the entry.</param>
        void Remove(TKey key);
    }
}
