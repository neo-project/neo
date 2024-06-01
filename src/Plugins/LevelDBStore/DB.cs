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

using LevelDB.NativePointer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LevelDB
{
    /// <summary>
    /// A DB is a persistent ordered map from keys to values.
    /// A DB is safe for concurrent access from multiple threads without any external synchronization.
    /// </summary>
    public class DB : LevelDBHandle, IEnumerable<KeyValuePair<string, string>>, IEnumerable<KeyValuePair<byte[], byte[]>>, IEnumerable<KeyValuePair<int, int[]>>
    {
        private static readonly Encoding s_uTF8 = Encoding.UTF8;
        public readonly static Encoding DefaultEncoding = s_uTF8;

        private readonly Cache _cache;
        private readonly Comparator _comparator;
        private readonly Encoding _encoding;

        static void Throw(IntPtr error)
        {
            Throw(error, msg => new Exception(msg));
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
                    LevelDBInterop.leveldb_free(error);
                }
            }
        }

        public DB(string name, Options options)
            : this(name, DefaultEncoding, options)
        {
        }

        /// <summary>
        /// Open the database with the specified "name".
        /// </summary>
        public DB(string name, Encoding encoding, Options options)
        {
            _cache = options.Cache;
            _comparator = options.Comparator;
            Handle = LevelDBInterop.leveldb_open(options.Handle, name, out var error);
            _encoding = encoding;

            Throw(error, msg => new UnauthorizedAccessException(msg));
        }

        public void Close()
        {
            (this as IDisposable).Dispose();
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Put(string key, string value, WriteOptions options)
        {
            Put(_encoding.GetBytes(key), _encoding.GetBytes(value), options);
        }

        /// <summary>
        /// Set the database entry for "key" to "value". 
        /// </summary>
        public void Put(string key, string value)
        {
            Put(key, value, new WriteOptions());
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
            LevelDBInterop.leveldb_put(Handle, options.Handle, key, checked((IntPtr)key.LongLength), value, checked((IntPtr)value.LongLength), out var error);
            Throw(error);
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Put(int key, int[] value)
        {
            Put(key, value, new WriteOptions());
        }

        /// <summary>
        /// Set the database entry for "key" to "value".  
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Put(int key, int[] value, WriteOptions options)
        {
            LevelDBInterop.leveldb_put(Handle, options.Handle, ref key, sizeof(int), value, checked((IntPtr)(value.LongLength * 4)), out var error);
            Throw(error);
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// </summary>
        public void Delete(string key)
        {
            Delete(key, new WriteOptions());
        }

        /// <summary>
        /// Remove the database entry (if any) for "key".  
        /// It is not an error if "key" did not exist in the database.
        /// Note: consider setting new WriteOptions{ Sync = true }.
        /// </summary>
        public void Delete(string key, WriteOptions options)
        {
            Delete(_encoding.GetBytes(key), options);
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
            LevelDBInterop.leveldb_delete(Handle, options.Handle, key, checked((IntPtr)key.LongLength), out var error);
            Throw(error);
        }

        public void Write(WriteBatch batch)
        {
            Write(batch, new WriteOptions());
        }

        public void Write(WriteBatch batch, WriteOptions options)
        {
            LevelDBInterop.leveldb_write(Handle, options.Handle, batch.Handle, out var error);
            Throw(error);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public string Get(string key, ReadOptions options)
        {
            var value = Get(_encoding.GetBytes(key), options);
            if (value != null) return _encoding.GetString(value);
            return null;
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public string Get(string key)
        {
            return Get(key, ReadOptions.Default);
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
            var v = LevelDBInterop.leveldb_get(Handle, options.Handle, key, checked((IntPtr)key.LongLength), out var length, out var error);
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
                    LevelDBInterop.leveldb_free(v);
                }
            }
            return null;
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public int[] Get(int key)
        {
            return Get(key, ReadOptions.Default);
        }

        /// <summary>
        /// If the database contains an entry for "key" return the value,
        /// otherwise return null.
        /// </summary>
        public int[] Get(int key, ReadOptions options)
        {

            IntPtr v;
            v = LevelDBInterop.leveldb_get(Handle, options.Handle, ref key, sizeof(int), out var length, out var error);
            Throw(error);

            if (v != IntPtr.Zero)
            {
                try
                {
                    var len = (long)length / 4;
                    int c = 0, si = 0;

                    var bytes = new int[len];

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
                    LevelDBInterop.leveldb_free(v);
                }
            }
            return null;
        }

        public bool Contains(byte[] key, ReadOptions options)
        {
            var value = LevelDBInterop.leveldb_get(Handle, options.Handle, key, key.Length, out _, out var error);
            Throw(error);

            if (value != IntPtr.Zero)
            {
                LevelDBInterop.leveldb_free(value);
                return true;
            }

            return false;
        }

        public NativeArray<T> GetRaw<T>(NativeArray key)
            where T : struct
        {
            return GetRaw<T>(key, ReadOptions.Default);
        }
        public NativeArray<T> GetRaw<T>(NativeArray key, ReadOptions options)
            where T : struct
        {

            var handle = new LevelDbFreeHandle();

            // todo: remove typecast to int
            var v = (Ptr<T>)LevelDBInterop.leveldb_get(
                Handle,
                options.Handle,
                key._baseAddr,
                key._byteLength,
                out var length,
                out _);

            handle.SetHandle((IntPtr)v);

            // round down, truncating the array slightly if needed
            var count = (IntPtr)((ulong)length / Ptr<T>.SizeofT);

            return new NativeArray<T> { _baseAddr = v, _count = count, _handle = handle };
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
            return new Iterator(LevelDBInterop.leveldb_create_iterator(Handle, options.Handle), _encoding);
        }

        /// <summary>
        /// Return a handle to the current DB state.  
        /// Iterators and Gets created with this handle will all observe a stable snapshot of the current DB state.  
        /// </summary>
        public SnapShot CreateSnapshot()
        {
            return new SnapShot(LevelDBInterop.leveldb_create_snapshot(Handle), this);
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
            var ptr = LevelDBInterop.leveldb_property_value(Handle, name);
            if (ptr != IntPtr.Zero)
            {
                try
                {
                    return Marshal.PtrToStringAnsi(ptr);
                }
                finally
                {
                    LevelDBInterop.leveldb_free(ptr);
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
            LevelDBInterop.leveldb_repair_db(options.Handle, name, out var error);
            Throw(error);
        }

        /// <summary>
        /// Destroy the contents of the specified database.
        /// Be very careful using this method.
        /// </summary>
        public static void Destroy(Options options, string name)
        {
            LevelDBInterop.leveldb_destroy_db(options.Handle, name, out var error);
            Throw(error);
        }

        protected override void FreeUnManagedObjects()
        {
            if (Handle != default)
                LevelDBInterop.leveldb_close(Handle);

            // it's critical that the database be closed first, as the logger and cache may depend on it.

            _cache?.Dispose();

            _comparator?.Dispose();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            using var sn = CreateSnapshot();
            using var iterator = CreateIterator(new ReadOptions { Snapshot = sn });
            iterator.SeekToFirst();
            while (iterator.IsValid())
            {
                yield return new KeyValuePair<string, string>(iterator.KeyAsString(), iterator.ValueAsString());
                iterator.Next();
            }
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

        IEnumerator<KeyValuePair<int, int[]>> IEnumerable<KeyValuePair<int, int[]>>.GetEnumerator()
        {
            using var sn = CreateSnapshot();
            using var iterator = CreateIterator(new ReadOptions { Snapshot = sn });
            iterator.SeekToFirst();
            while (iterator.IsValid())
            {
                yield return new KeyValuePair<int, int[]>(iterator.KeyAsInt(), iterator.ValueAsInts());
                iterator.Next();
            }
        }
    }
}
