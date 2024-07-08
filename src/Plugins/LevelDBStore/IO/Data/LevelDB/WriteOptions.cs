// Copyright (C) 2015-2024 The Neo Project.
//
// WriteOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.IO.Data.LevelDB
{
    public class WriteOptions
    {
        public static readonly WriteOptions Default = new WriteOptions();
        public static readonly WriteOptions SyncWrite = new WriteOptions { Sync = true };

        internal readonly nint handle = Native.leveldb_writeoptions_create();

        public bool Sync
        {
            set
            {
                Native.leveldb_writeoptions_set_sync(handle, value);
            }
        }

        ~WriteOptions()
        {
            Native.leveldb_writeoptions_destroy(handle);
        }
    }
}
