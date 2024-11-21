// Copyright (C) 2015-2024 The Neo Project.
//
// ReadOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IO.Data.LevelDB
{
    /// <summary>
    /// Options that control read operations.
    /// </summary>
    public class ReadOptions : LevelDBHandle
    {
        public static readonly ReadOptions Default = new();

        public ReadOptions() : base(Native.leveldb_readoptions_create()) { }

        /// <summary>
        /// If true, all data read from underlying storage will be
        /// verified against corresponding checksums.
        /// </summary>
        public bool VerifyChecksums
        {
            set { Native.leveldb_readoptions_set_verify_checksums(Handle, value); }
        }

        /// <summary>
        /// Should the data read for this iteration be cached in memory?
        /// Callers may wish to set this field to false for bulk scans.
        /// Default: true
        /// </summary>
        public bool FillCache
        {
            set { Native.leveldb_readoptions_set_fill_cache(Handle, value); }
        }

        /// <summary>
        /// If "snapshot" is provided, read as of the supplied snapshot
        /// (which must belong to the DB that is being read and which must
        /// not have been released).
        /// If "snapshot" is not set, use an implicit
        /// snapshot of the state at the beginning of this read operation.
        /// </summary>
        public Snapshot Snapshot
        {
            set { Native.leveldb_readoptions_set_snapshot(Handle, value.Handle); }
        }

        protected override void FreeUnManagedObjects()
        {
            Native.leveldb_readoptions_destroy(Handle);
        }
    }
}
