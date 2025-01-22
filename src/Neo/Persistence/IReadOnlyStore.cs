// Copyright (C) 2015-2025 The Neo Project.
//
// IReadOnlyStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods to read from the database.
    /// </summary>
    public interface IReadOnlyStore
    {
        /// <summary>
        /// Serialized cache
        /// </summary>
        SerializedCache SerializedCache { get; }

        /// <summary>
        /// Seeks to the entry with the specified key.
        /// </summary>
        /// <param name="keyOrPrefix">The key(i.e. start key) or prefix to be sought.</param>
        /// <param name="direction">The direction of seek.</param>
        /// <returns>An enumerator containing all the entries after (Forward) or before(Backward) seeking.</returns>
        IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[]? keyOrPrefix, SeekDirection direction);

        /// <summary>
        /// Reads a specified entry from the database.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <returns>The data of the entry. Or <see langword="null"/> if it doesn't exist.</returns>
        [Obsolete("use TryGet(byte[] key, [NotNullWhen(true)] out byte[]? value) instead.")]
        byte[]? TryGet(byte[] key);

        /// <summary>
        /// Reads a specified entry from the database.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <param name="value">The data of the entry.</param>
        /// <returns><see langword="true"/> if the entry exists; otherwise, <see langword="false"/>.</returns>
        bool TryGet(byte[] key, [NotNullWhen(true)] out byte[]? value);

        /// <summary>
        /// Determines whether the database contains the specified entry.
        /// </summary>
        /// <param name="key">The key of the entry.</param>
        /// <returns><see langword="true"/> if the database contains an entry with the specified key; otherwise, <see langword="false"/>.</returns>
        bool Contains(byte[] key);
    }
}
