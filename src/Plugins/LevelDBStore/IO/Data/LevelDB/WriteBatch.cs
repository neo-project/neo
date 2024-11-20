// Copyright (C) 2015-2024 The Neo Project.
//
// WriteBatch.cs file belongs to the neo project and is free
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
    /// WriteBatch holds a collection of updates to apply atomically to a DB.
    ///
    /// The updates are applied in the order in which they are added
    /// to the WriteBatch.  For example, the value of "key" will be "v3"
    /// after the following batch is written:
    ///
    ///    batch.Put("key", "v1");
    ///    batch.Delete("key");
    ///    batch.Put("key", "v2");
    ///    batch.Put("key", "v3");
    /// </summary>
    public class WriteBatch : LevelDBHandle
    {

        public WriteBatch()
        {
            Handle = Native.leveldb_writebatch_create();
        }

        /// <summary>
        /// Clear all updates buffered in this batch.
        /// </summary>
        public void Clear()
        {
            Native.leveldb_writebatch_clear(Handle);
        }

        /// <summary>
        /// Store the mapping "key->value" in the database.
        /// </summary>
        public void Put(byte[] key, byte[] value)
        {
            Native.leveldb_writebatch_put(Handle, key, (UIntPtr)key.Length, value, (UIntPtr)value.Length);
        }

        /// <summary>
        /// If the database contains a mapping for "key", erase it.
        /// Else do nothing.
        /// </summary>
        public void Delete(byte[] key)
        {
            Native.leveldb_writebatch_delete(Handle, key, (UIntPtr)key.Length);
        }

        protected override void FreeUnManagedObjects()
        {
            Native.leveldb_writebatch_destroy(Handle);
        }
    }
}
