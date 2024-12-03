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

namespace Neo.IO.Storage.LevelDB
{
    /// <summary>
    /// An iterator yields a sequence of key/value pairs from a database.
    /// </summary>
    public class Iterator : LevelDBHandle
    {
        internal Iterator(IntPtr handle) : base(handle) { }

        private void CheckError()
        {
            Native.leveldb_iter_get_error(Handle, out var error);
            NativeHelper.CheckError(error);
        }

        protected override void FreeUnManagedObjects()
        {
            if (Handle != IntPtr.Zero)
            {
                Native.leveldb_iter_destroy(Handle);
            }
        }

        /// <summary>
        /// Return the key for the current entry.
        /// REQUIRES: Valid()
        /// </summary>
        public byte[] Key()
        {
            var key = Native.leveldb_iter_key(Handle, out var length);
            CheckError();
            return key.ToByteArray(length);
        }

        /// <summary>
        /// Moves to the next entry in the source.
        /// After this call, Valid() is true if the iterator was not positioned at the last entry in the source.
        /// REQUIRES: Valid()
        /// </summary>
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

        /// <summary>
        /// Position at the first key in the source that at or past target
        /// The iterator is Valid() after this call if the source contains
        /// an entry that comes at or past target.
        /// </summary>
        public void Seek(byte[] target)
        {
            Native.leveldb_iter_seek(Handle, target, (UIntPtr)target.Length);
        }

        public void SeekToFirst()
        {
            Native.leveldb_iter_seek_to_first(Handle);
        }

        /// <summary>
        /// Position at the last key in the source.
        /// The iterator is Valid() after this call if the source is not empty.
        /// </summary>
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
            return value.ToByteArray(length);
        }
    }
}
