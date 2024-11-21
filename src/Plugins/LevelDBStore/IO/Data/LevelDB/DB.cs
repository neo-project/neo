// Copyright (C) 2015-2024 The Neo Project.
//
// DB.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;

namespace Neo.IO.Data.LevelDB
{
    /// <summary>
    /// A DB is a persistent ordered map from keys to values.
    /// A DB is safe for concurrent access from multiple threads without any external synchronization.
    /// </summary>
    public class DB : LevelDBHandle
    {
        private DB(IntPtr handle) : base(handle) { }

        protected override void FreeUnManagedObjects()
        {
            if (Handle != IntPtr.Zero)
            {
                Native.leveldb_close(Handle);
            }
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".
        /// It is not an error if "key" did not exist in the database.
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Delete(WriteOptions options, byte[] key)
        {
            Native.leveldb_delete(Handle, options.Handle, key, (UIntPtr)key.Length, out var error);
            NativeHelper.CheckError(error);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public byte[] Get(ReadOptions options, byte[] key)
        {
            var value = Native.leveldb_get(Handle, options.Handle, key, (UIntPtr)key.Length, out var length, out var error);
            try
            {
                NativeHelper.CheckError(error);
                return value.ToByteArray(length);
            }
            finally
            {
                if (value != IntPtr.Zero) Native.leveldb_free(value);
            }
        }

        public bool Contains(ReadOptions options, byte[] key)
        {
            var value = Native.leveldb_get(Handle, options.Handle, key, (UIntPtr)key.Length, out _, out var error);
            NativeHelper.CheckError(error);

            if (value != IntPtr.Zero)
            {
                Native.leveldb_free(value);
                return true;
            }

            return false;
        }

        public Snapshot GetSnapshot()
        {
            return new Snapshot(Handle);
        }

        public Iterator NewIterator(ReadOptions options)
        {
            return new Iterator(Native.leveldb_create_iterator(Handle, options.Handle));
        }

        public static DB Open(string name)
        {
            return Open(name, Options.Default);
        }

        public static DB Open(string name, Options options)
        {
            var Handle = Native.leveldb_open(options.Handle, Path.GetFullPath(name), out var error);
            NativeHelper.CheckError(error);
            return new DB(Handle);
        }

        /// <summary>
        /// Set the database entry for "key" to "value".
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Put(WriteOptions options, byte[] key, byte[] value)
        {
            Native.leveldb_put(Handle, options.Handle, key, (UIntPtr)key.Length, value, (UIntPtr)value.Length, out var error);
            NativeHelper.CheckError(error);
        }

        /// <summary>
        /// If a DB cannot be opened, you may attempt to call this method to
        /// resurrect as much of the contents of the database as possible.
        /// Some data may be lost, so be careful when calling this function
        /// on a database that contains important information.
        /// </summary>
        public static void Repair(string name, Options options)
        {
            Native.leveldb_repair_db(options.Handle, Path.GetFullPath(name), out var error);
            NativeHelper.CheckError(error);
        }

        public void Write(WriteOptions options, WriteBatch write_batch)
        {
            Native.leveldb_write(Handle, options.Handle, write_batch.Handle, out var error);
            NativeHelper.CheckError(error);
        }
    }
}
