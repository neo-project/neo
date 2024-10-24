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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Neo.IO.Storage.LevelDB
{
    /// <summary>
    /// A DB is a persistent ordered map from keys to values.
    /// A DB is safe for concurrent access from multiple threads without any external synchronization.
    /// </summary>
    public class DB : LevelDBHandle, IEnumerable<KeyValuePair<byte[], byte[]>>
    {
        static void Throw(IntPtr error)
        {
            Throw(error, msg => new LevelDBException(msg));
        }

        static void Throw(IntPtr error, Func<string, Exception> exception)
        {
            if (error != IntPtr.Zero)
            {
                try
                {
                    var msg = Marshal.PtrToStringAnsi(error);
                    throw exception(msg);
                }
                finally
                {
                    Native.leveldb_free(error);
                }
            }
        }

        /// <summary>
        /// Open the database with the specified "name".
        /// </summary>
        public DB(string name, Options options)
        {
            Handle = Native.leveldb_open(options.Handle, name, out var error);
            Throw(error, msg => new UnauthorizedAccessException(msg));
        }

        public void Close()
        {
            (this as IDisposable).Dispose();
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// </summary>
        public void Put(byte[] key, byte[] value)
        {
            Put(key, value, new WriteOptions());
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Put(byte[] key, byte[] value, WriteOptions options)
        {
            Native.leveldb_put(Handle, options.Handle, key, checked((IntPtr)key.LongLength), value, checked((IntPtr)value.LongLength), out var error);
            Throw(error);
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// </summary>
        public void Delete(byte[] key)
        {
            Delete(key, new WriteOptions());
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Delete(byte[] key, WriteOptions options)
        {
            Native.leveldb_delete(Handle, options.Handle, key, checked((nint)key.LongLength), out var error);
            Throw(error);
        }

        public void Write(WriteBatch batch)
        {
            Write(batch, new WriteOptions());
        }

        public void Write(WriteBatch batch, WriteOptions options)
        {
            Native.leveldb_write(Handle, options.Handle, batch.Handle, out var error);
            Throw(error);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public byte[] Get(byte[] key)
        {
            return Get(key, ReadOptions.Default);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public byte[] Get(byte[] key, ReadOptions options)
        {
            var v = Native.leveldb_get(Handle, options.Handle, key, checked((IntPtr)key.LongLength), out var length, out var error);
            Throw(error);

            if (v != IntPtr.Zero)
            {
                try
                {

                    var len = (long)length;
                    int c = 0, si = 0;

                    var bytes = new byte[len];

                    while (si < len)
                    {
                        if (len - si > int.MaxValue)
                            c += int.MaxValue;
                        else
                            c += checked((int)(len - si));

                        // Method has a ~2GB limit.
                        Marshal.Copy(v, bytes, si, c);

                        si += c;
                    }
                    return bytes;
                }
                finally
                {
                    Native.leveldb_free(v);
                }
            }
            return null;
        }

        public bool Contains(byte[] key, ReadOptions options)
        {
            var value = Native.leveldb_get(Handle, options.Handle, key, key.Length, out _, out var error);
            Throw(error);

            if (value != IntPtr.Zero)
            {
                Native.leveldb_free(value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return an iterator over the contents of the database.
        /// The result of CreateIterator is initially invalid (caller must
        /// call one of the Seek methods on the iterator before using it).
        /// </summary>
        public Iterator CreateIterator()
        {
            return CreateIterator(ReadOptions.Default);
        }

        /// <summary>
        /// Return an iterator over the contents of the database.
        /// The result of CreateIterator is initially invalid (caller must
        /// call one of the Seek methods on the iterator before using it).
        /// </summary>
        public Iterator CreateIterator(ReadOptions options)
        {
            return new Iterator(Native.leveldb_create_iterator(Handle, options.Handle));
        }

        /// <summary>
        /// Return a handle to the current DB state.  
        /// Iterators and Gets created with this handle will all observe a stable snapshot of the current DB state.  
        /// </summary>
        public SnapShot CreateSnapshot()
        {
            return new SnapShot(Native.leveldb_create_snapshot(Handle), this);
        }

        /// <summary>
        /// DB implementations can export properties about their state
        /// via this method.  If "property" is a valid property understood by this
        /// DB implementation, fills "*value" with its current value and returns
        /// true.  Otherwise returns false.
        ///
        /// Valid property names include:
        ///
        ///  "leveldb.num-files-at-level<N>" - return the number of files at level </N>,
        ///     where <N> is an ASCII representation of a level number (e.g. "0")</N>.
        ///  "leveldb.stats" - returns a multi-line string that describes statistics
        ///     about the internal operation of the DB.
        /// </summary>
        public string PropertyValue(string name)
        {
            string result = null;
            var ptr = Native.leveldb_property_value(Handle, name);
            if (ptr != IntPtr.Zero)
            {
                try
                {
                    return Marshal.PtrToStringAnsi(ptr);
                }
                finally
                {
                    Native.leveldb_free(ptr);
                }
            }
            return result;
        }

        /// <summary>
        /// If a DB cannot be opened, you may attempt to call this method to
        /// resurrect as much of the contents of the database as possible.
        /// Some data may be lost, so be careful when calling this function
        /// on a database that contains important information.
        /// </summary>
        public static void Repair(Options options, string name)
        {
            Native.leveldb_repair_db(options.Handle, name, out var error);
            Throw(error);
        }

        /// <summary>
        /// Destroy the contents of the specified database.
        /// Be very careful using this method.
        /// </summary>
        public static void Destroy(Options options, string name)
        {
            Native.leveldb_destroy_db(options.Handle, name, out var error);
            Throw(error);
        }

        protected override void FreeUnManagedObjects()
        {
            if (Handle != default)
                Native.leveldb_close(Handle);
        }

        public IEnumerator<KeyValuePair<byte[], byte[]>> GetEnumerator()
        {
            using var sn = CreateSnapshot();
            using var iterator = CreateIterator(new ReadOptions { Snapshot = sn });
            iterator.SeekToFirst();
            while (iterator.IsValid())
            {
                yield return new KeyValuePair<byte[], byte[]>(iterator.Key(), iterator.Value());
                iterator.Next();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
