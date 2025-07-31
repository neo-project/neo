// Copyright (C) 2015-2025 The Neo Project.
//
// StorageStats.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IEventHandlers
{
    /// <summary>
    /// Storage statistics snapshot
    /// </summary>
    public class StorageStats
    {
        public long TotalReads { get; set; }
        public long TotalWrites { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double CacheHitRate => TotalReads > 0 ? (double)CacheHits / TotalReads : 0;
        public long StorageSizeBytes { get; set; }
        public int SnapshotCount { get; set; }
    }
}
