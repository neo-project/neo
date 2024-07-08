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

using Neo.Plugins.Storage.IO.Data.LevelDB;
using System.IO;

namespace Neo.IO.Data.LevelDB
{
    public class DB : LevelDBHandle
    {
        public DB(nint handle)
        {
            Handle = handle;
        }

        public void Delete(WriteOptions options, byte[] key)
        {
            Native.leveldb_delete(Handle, options.handle, key, key.Length, out var error);
            NativeHelper.CheckError(error);
        }

        public byte[] Get(ReadOptions options, byte[] key)
        {
            var value = Native.leveldb_get(Handle, options.handle, key, key.Length, out var length, out var error);
            try
            {
                NativeHelper.CheckError(error);
                return value.ToByteArray((nuint)length);
            }
            finally
            {
                if (value != nint.Zero) Native.leveldb_free(value);
            }
        }

        public bool Contains(ReadOptions options, byte[] key)
        {
            var value = Native.leveldb_get(Handle, options.handle, key, key.Length, out _, out var error);
            NativeHelper.CheckError(error);

            if (value != nint.Zero)
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
            return new Iterator(Native.leveldb_create_iterator(Handle, options.handle));
        }

        public static DB Open(string name)
        {
            return Open(name, Options.Default);
        }

        public static DB Open(string name, Options options)
        {
            var handle = Native.leveldb_open(options.handle, Path.GetFullPath(name), out var error);
            NativeHelper.CheckError(error);
            return new DB(handle);
        }

        public void Put(WriteOptions options, byte[] key, byte[] value)
        {
            Native.leveldb_put(Handle, options.handle, key, key.Length, value, value.Length, out var error);
            NativeHelper.CheckError(error);
        }

        public static void Repair(string name, Options options)
        {
            Native.leveldb_repair_db(options.handle, Path.GetFullPath(name), out var error);
            NativeHelper.CheckError(error);
        }

        public void Write(WriteOptions options, WriteBatch write_batch)
        {
            Native.leveldb_write(Handle, options.handle, write_batch.Handle, out var error);
            NativeHelper.CheckError(error);
        }
    }
}
