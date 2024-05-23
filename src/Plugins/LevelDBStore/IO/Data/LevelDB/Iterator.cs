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

using System;

namespace Neo.IO.Data.LevelDB
{
    public class Iterator : IDisposable
    {
        private IntPtr handle;

        internal Iterator(IntPtr handle)
        {
            this.handle = handle;
        }

        private void CheckError()
        {
            Native.leveldb_iter_get_error(handle, out IntPtr error);
            NativeHelper.CheckError(error);
        }

        public void Dispose()
        {
            if (handle != IntPtr.Zero)
            {
                Native.leveldb_iter_destroy(handle);
                handle = IntPtr.Zero;
            }
        }

        public byte[] Key()
        {
            IntPtr key = Native.leveldb_iter_key(handle, out UIntPtr length);
            CheckError();
            return key.ToByteArray(length);
        }

        public void Next()
        {
            Native.leveldb_iter_next(handle);
            CheckError();
        }

        public void Prev()
        {
            Native.leveldb_iter_prev(handle);
            CheckError();
        }

        public void Seek(byte[] target)
        {
            Native.leveldb_iter_seek(handle, target, (UIntPtr)target.Length);
        }

        public void SeekToFirst()
        {
            Native.leveldb_iter_seek_to_first(handle);
        }

        public void SeekToLast()
        {
            Native.leveldb_iter_seek_to_last(handle);
        }

        public bool Valid()
        {
            return Native.leveldb_iter_valid(handle);
        }

        public byte[] Value()
        {
            IntPtr value = Native.leveldb_iter_value(handle, out UIntPtr length);
            CheckError();
            return value.ToByteArray(length);
        }
    }
}
