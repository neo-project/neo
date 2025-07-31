// Copyright (C) 2015-2025 The Neo Project.
//
// IStorageMetricsHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using System;

namespace Neo.IEventHandlers
{
    /// <summary>
    /// Interface for plugins that need to collect storage metrics
    /// </summary>
    public interface IStorageMetricsHandler
    {
        /// <summary>
        /// Called when a storage read operation occurs
        /// </summary>
        /// <param name="store">The store instance</param>
        /// <param name="key">The key being read</param>
        /// <param name="found">Whether the key was found</param>
        /// <param name="duration">Time taken for the operation</param>
        void Storage_Read_Handler(IStore store, byte[] key, bool found, TimeSpan duration);

        /// <summary>
        /// Called when a storage write operation occurs
        /// </summary>
        /// <param name="store">The store instance</param>
        /// <param name="key">The key being written</param>
        /// <param name="valueSize">Size of the value in bytes</param>
        /// <param name="duration">Time taken for the operation</param>
        void Storage_Write_Handler(IStore store, byte[] key, int valueSize, TimeSpan duration);

        /// <summary>
        /// Called periodically with storage statistics
        /// </summary>
        /// <param name="store">The store instance</param>
        /// <param name="stats">Current storage statistics</param>
        void Storage_StatsSnapshot_Handler(IStore store, StorageStats stats);
    }
}
