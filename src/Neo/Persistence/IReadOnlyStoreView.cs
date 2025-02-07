// Copyright (C) 2015-2025 The Neo Project.
//
// IReadOnlyStoreView.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using Neo.SmartContract;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Persistence
{
    /// <summary>
    /// Represents a read-only store view.
    /// Caching or not depends on the implementation.
    /// Implementaion should be thread-safe.
    /// </summary>
    public interface IReadOnlyStoreView
    {
        /// <summary>
        /// Checks if the store view contains an entry with the specified key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the entry exists, false otherwise.</returns>
        bool Contains(StorageKey key);

        /// <summary>
        /// Gets the entry with the specified key.
        /// </summary>
        /// <param name="key">The key to get.</param>
        /// <returns>The entry if found, throws a <see cref="KeyNotFoundException"/> otherwise.</returns>
        StorageItem this[StorageKey key] { get; }

        /// <summary>
        /// Tries to get the entry with the specified key.
        /// </summary>
        /// <param name="key">The key to get.</param>
        /// <param name="item">The entry if found, null otherwise.</param>
        /// <returns>True if the entry exists, false otherwise.</returns>
        bool TryGet(StorageKey key, [NotNullWhen(true)] out StorageItem? item);
    }
}
