// Copyright (C) 2015-2025 The Neo Project.
//
// IStoreSnapshot.cs file belongs to the neo project and is free
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

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods for reading, writing, and committing from/to snapshot.
    /// </summary>
    public interface IStoreSnapshot :
        IReadOnlyStore<byte[], byte[]>,
        IWriteStore<byte[], byte[]>,
        IDisposable
    {
        /// <summary>
        /// Store
        /// </summary>
        IStore Store { get; }

        /// <summary>
        /// Commits all changes in the snapshot to the database.
        /// </summary>
        void Commit();

        /// <summary>
        /// Seeks to the entry with the specified key.
        /// </summary>
        /// <param name="keyOrPrefix">The key(i.e. start key) or prefix to be sought.</param>
        /// <param name="direction">The direction of seek.</param>
        /// <returns>An enumerator containing all the entries after (Forward) or before(Backward) seeking.</returns>
        IEnumerable<(byte[] Key, byte[] Value)> Seek(byte[]? keyOrPrefix, SeekDirection direction);
    }
}
