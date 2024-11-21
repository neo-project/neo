// Copyright (C) 2015-2024 The Neo Project.
//
// Snapshot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.IO.Data.LevelDB
{
    /// <summary>
    /// A Snapshot is an immutable object and can therefore be safely
    /// accessed from multiple threads without any external synchronization.
    /// </summary>
    public class Snapshot : LevelDBHandle
    {
        internal IntPtr _db;

        internal Snapshot(IntPtr db) : base(Native.leveldb_create_snapshot(db))
        {
            _db = db;
        }

        protected override void FreeUnManagedObjects()
        {
            if (Handle != IntPtr.Zero)
            {
                Native.leveldb_release_snapshot(_db, Handle);
            }
        }
    }
}
