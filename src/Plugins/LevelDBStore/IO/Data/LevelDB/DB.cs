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
    public class DB : IDisposable
    {
        private IntPtr handle;

        /// <summary>
        /// Return true if haven't got valid handle
        /// </summary>
        public bool IsDisposed => handle == IntPtr.Zero;

        private DB(IntPtr handle)
        {
            this.handle = handle;
        }

        public void Dispose()
        {
            if (handle != IntPtr.Zero)
            {
                Native.leveldb_close(handle);
                handle = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".
        /// It is not an error if "key" did not exist in the database.
        /// </summary>
        public void Delete(byte[] key)
        {
            Delete(WriteOptions.Default, key);
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".
        /// It is not an error if "key" did not exist in the database.
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Delete(WriteOptions options, byte[] key)
        {
            Native.leveldb_delete(handle, options.handle, key, (UIntPtr)key.Length, out IntPtr error);
            NativeHelper.CheckError(error);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public byte[] Get(byte[] key)
        {
            return Get(ReadOptions.Default, key);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public byte[] Get(ReadOptions options, byte[] key)
        {
            IntPtr value = Native.leveldb_get(handle, options.handle, key, (UIntPtr)key.Length, out UIntPtr length, out IntPtr error);
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
            IntPtr value = Native.leveldb_get(handle, options.handle, key, (UIntPtr)key.Length, out _, out IntPtr error);
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
            return new Snapshot(handle);
        }

        public Iterator NewIterator(ReadOptions options)
        {
            return new Iterator(Native.leveldb_create_iterator(handle, options.handle));
        }

        public static DB Open(string name)
        {
            return Open(name, Options.Default);
        }

        public static DB Open(string name, Options options)
        {
            IntPtr handle = Native.leveldb_open(options.handle, Path.GetFullPath(name), out IntPtr error);
            NativeHelper.CheckError(error);
            return new DB(handle);
        }

        /// <summary>
        /// Set the database entry for "key" to "value".
        /// </summary>
        public void Put(byte[] key, byte[] value)
        {
            Put(WriteOptions.Default, key, value);
        }

        /// <summary>
        /// Set the database entry for "key" to "value".
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Put(WriteOptions options, byte[] key, byte[] value)
        {
            Native.leveldb_put(handle, options.handle, key, (UIntPtr)key.Length, value, (UIntPtr)value.Length, out IntPtr error);
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
            Native.leveldb_repair_db(options.handle, Path.GetFullPath(name), out IntPtr error);
            NativeHelper.CheckError(error);
        }

        public void Write(WriteOptions options, WriteBatch write_batch)
        {
            Native.leveldb_write(handle, options.handle, write_batch.handle, out IntPtr error);
            NativeHelper.CheckError(error);
        }
    }
}
