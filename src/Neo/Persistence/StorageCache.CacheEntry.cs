// Copyright (C) 2015-2025 The Neo Project.
//
// StorageCache.CacheEntry.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Persistence
{
    public partial class StorageCache
    {
        /// <summary>
        /// Represents state of an entry in the cache.
        /// </summary>
        public class CacheEntry(
            [DisallowNull] StorageKey key,
            [AllowNull] StorageItem value,
            [DisallowNull] TrackState state)
        {
            /// <summary>
            /// The key of the cached entry.
            /// </summary>
            public StorageKey Key { [return: NotNull] get; [param: DisallowNull] internal set; } = key;

            /// <summary>
            /// The data of the cached entry.
            /// </summary>
            [AllowNull]
            public StorageItem Value { [return: MaybeNull] get; [param: AllowNull] internal set; } = value;

            /// <summary>
            /// The state of the cached entry.
            /// </summary>
            public TrackState State { [return: NotNull] get; [param: DisallowNull] internal set; } = state;
        }
    }
}
