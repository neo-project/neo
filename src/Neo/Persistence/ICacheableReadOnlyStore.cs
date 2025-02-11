// Copyright (C) 2015-2025 The Neo Project.
//
// ICacheableReadOnlyStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.SmartContract;
using System;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods to read from the database.
    /// </summary>
    public interface ICacheableReadOnlyStore : ICacheableReadOnlyStore<StorageKey, StorageItem>, IReadOnlyStore { }

    /// <summary>
    /// This interface provides methods to read from the database.
    /// </summary>
    public interface ICacheableReadOnlyStore<TKey, TValue> : IReadOnlyStore<TKey, TValue>
    {
        /// <summary>
        /// Tries to get the entry from cache.
        /// </summary>
        /// <typeparam name="T">Cache type</typeparam>
        /// <returns>The entry if found, null otherwise.</returns>
        public T? GetFromCache<T>() where T : IStorageCacheEntry;

        /// <summary>
        /// Adds a new entry to the cache.
        /// </summary>
        /// <param name="value">The data of the entry.</param>
        /// <exception cref="ArgumentException">The entry has already been cached.</exception>
        /// <remarks>Note: This method does not read the internal storage to check whether the record already exists.</remarks>
        public void AddToCache<T>(T? value = default) where T : IStorageCacheEntry;
    }
}
