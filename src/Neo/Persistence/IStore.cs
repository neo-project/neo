// Copyright (C) 2015-2026 The Neo Project.
//
// IStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Persistence
{
    /// <summary>
    /// This interface provides methods for reading, writing from/to database. Developers should implement this interface to provide new storage engines for NEO.
    /// </summary>
    public interface IStore :
        IReadOnlyStore<byte[], byte[]>,
        IWriteStore<byte[], byte[]>,
        IDisposable
    {
        /// <summary>
        /// Delegate for OnNewSnapshot
        /// </summary>
        /// <param name="sender">Store</param>
        /// <param name="snapshot">Snapshot</param>
        public delegate void OnNewSnapshotDelegate(IStore sender, IStoreSnapshot snapshot);

        /// <summary>
        /// Event raised when a new snapshot is created
        /// </summary>
        public event OnNewSnapshotDelegate? OnNewSnapshot;

        /// <summary>
        /// Creates a snapshot of the database.
        /// </summary>
        /// <returns>A snapshot of the database.</returns>
        IStoreSnapshot GetSnapshot();
    }
}
