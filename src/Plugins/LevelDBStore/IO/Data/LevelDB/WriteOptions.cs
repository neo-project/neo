// Copyright (C) 2015-2025 The Neo Project.
//
// WriteOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IO.Storage.LevelDB
{
    /// <summary>
    /// Options that control write operations.
    /// </summary>
    public class WriteOptions : LevelDBHandle
    {
        public static readonly WriteOptions Default = new();
        public static readonly WriteOptions SyncWrite = new() { Sync = true };

        public WriteOptions() : base(Native.leveldb_writeoptions_create()) { }

        /// <summary>
        /// If true, the write will be flushed from the operating system
        /// buffer cache (by calling WritableFile::Sync()) before the write
        /// is considered complete.  If this flag is true, writes will be
        /// slower.
        ///
        /// If this flag is false, and the machine crashes, some recent
        /// writes may be lost.  Note that if it is just the process that
        /// crashes (i.e., the machine does not reboot), no writes will be
        /// lost even if sync==false.
        ///
        /// In other words, a DB write with sync==false has similar
        /// crash semantics as the "write()" system call.  A DB write
        /// with sync==true has similar crash semantics to a "write()"
        /// system call followed by "fsync()".
        /// </summary>
        public bool Sync
        {
            set { Native.leveldb_writeoptions_set_sync(Handle, value); }
        }

        protected override void FreeUnManagedObjects()
        {
            Native.leveldb_writeoptions_destroy(Handle);
        }
    }
}
