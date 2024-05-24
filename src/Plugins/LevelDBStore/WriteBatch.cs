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
using System.Text;

namespace LevelDB
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
        private readonly Encoding _encoding;

        public WriteBatch()
            : this(DB.DefaultEncoding)
        {
        }

        public WriteBatch(Encoding encoding)
        {
            _encoding = encoding;
            Handle = LevelDBInterop.leveldb_writebatch_create();
        }

        /// <summary>
        /// Clear all updates buffered in this batch.
        /// </summary>
        public void Clear()
        {
            LevelDBInterop.leveldb_writebatch_clear(Handle);
        }

        /// <summary>
        /// Store the mapping "key->value" in the database.
        /// </summary>
        public WriteBatch Put(string key, string value)
        {
            return Put(_encoding.GetBytes(key), _encoding.GetBytes(value));
        }

        /// <summary>
        /// Store the mapping "key->value" in the database.
        /// </summary>
        public WriteBatch Put(byte[] key, byte[] value)
        {
            LevelDBInterop.leveldb_writebatch_put(Handle, key, key.Length, value, value.Length);
            return this;
        }

        /// <summary>
        /// If the database contains a mapping for "key", erase it.  
        /// Else do nothing.
        /// </summary>
        public WriteBatch Delete(string key)
        {
            return Delete(_encoding.GetBytes(key));
        }

        /// <summary>
        /// If the database contains a mapping for "key", erase it.  
        /// Else do nothing.
        /// </summary>
        public WriteBatch Delete(byte[] key)
        {
            LevelDBInterop.leveldb_writebatch_delete(Handle, key, key.Length);
            return this;
        }

        /// <summary>
        /// Support for iterating over a batch.
        /// </summary>
        public void Iterate(object state, Action<object, byte[], int, byte[], int> put, Action<object, byte[], int> deleted)
        {
            LevelDBInterop.leveldb_writebatch_iterate(Handle, state, put, deleted);
        }

        protected override void FreeUnManagedObjects()
        {
            LevelDBInterop.leveldb_writebatch_destroy(Handle);
        }

    }
}
