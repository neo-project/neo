// Copyright (C) 2015-2024 The Neo Project.
//
// Iterator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.Storage.IO.Data.LevelDB;

namespace Neo.IO.Data.LevelDB
{
    public class Iterator : LevelDBHandle
    {
        internal Iterator(nint handle)
        {
            Handle = handle;
        }

        private void CheckError()
        {
            Native.leveldb_iter_get_error(Handle, out nint error);
            NativeHelper.CheckError(error);
        }

        public byte[] Key()
        {
            var key = Native.leveldb_iter_key(Handle, out var length);
            CheckError();
            return key.ToByteArray((nuint)length);
        }

        public void Next()
        {
            Native.leveldb_iter_next(Handle);
            CheckError();
        }

        public void Prev()
        {
            Native.leveldb_iter_prev(Handle);
            CheckError();
        }

        public void Seek(byte[] target)
        {
            Native.leveldb_iter_seek(Handle, target, target.Length);
        }

        public void SeekToFirst()
        {
            Native.leveldb_iter_seek_to_first(Handle);
        }

        public void SeekToLast()
        {
            Native.leveldb_iter_seek_to_last(Handle);
        }

        public bool Valid()
        {
            return Native.leveldb_iter_valid(Handle);
        }

        public byte[] Value()
        {
            var value = Native.leveldb_iter_value(Handle, out var length);
            CheckError();
            return value.ToByteArray((nuint)length);
        }
    }
}
