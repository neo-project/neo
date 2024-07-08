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
using System.Runtime.InteropServices;

namespace Neo.IO.Storage.LevelDB
{
    /// <summary>
    /// An iterator yields a sequence of key/value pairs from a database.
    /// </summary>
    public class Iterator : LevelDBHandle
    {
        internal Iterator(nint handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// An iterator is either positioned at a key/value pair, or
        /// not valid.  
        /// </summary>
        /// <returns>This method returns true iff the iterator is valid.</returns>
        public bool IsValid()
        {
            return Native.leveldb_iter_valid(Handle);
        }

        /// <summary>
        /// Position at the first key in the source.  
        /// The iterator is Valid() after this call iff the source is not empty.
        /// </summary>
        public void SeekToFirst()
        {
            Native.leveldb_iter_seek_to_first(Handle);
            Throw();
        }

        /// <summary>
        /// Position at the last key in the source.  
        /// The iterator is Valid() after this call iff the source is not empty.
        /// </summary>
        public void SeekToLast()
        {
            Native.leveldb_iter_seek_to_last(Handle);
            Throw();
        }

        /// <summary>
        /// Position at the first key in the source that at or past target
        /// The iterator is Valid() after this call iff the source contains
        /// an entry that comes at or past target.
        /// </summary>
        public void Seek(byte[] key)
        {
            Native.leveldb_iter_seek(Handle, key, key.Length);
            Throw();
        }

        /// <summary>
        /// Moves to the next entry in the source.  
        /// After this call, Valid() is true iff the iterator was not positioned at the last entry in the source.
        /// REQUIRES: Valid()
        /// </summary>
        public void Next()
        {
            Native.leveldb_iter_next(Handle);
            Throw();
        }

        /// <summary>
        /// Moves to the previous entry in the source.  
        /// After this call, Valid() is true iff the iterator was not positioned at the first entry in source.
        /// REQUIRES: Valid()
        /// </summary>
        public void Prev()
        {
            Native.leveldb_iter_prev(Handle);
            Throw();
        }

        /// <summary>
        /// Return the key for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public byte[] Key()
        {
            var key = Native.leveldb_iter_key(Handle, out var length);
            Throw();

            var bytes = new byte[length];
            Marshal.Copy(key, bytes, 0, length);
            return bytes;
        }

        /// <summary>
        /// Return the value for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public byte[] Value()
        {
            var value = Native.leveldb_iter_value(Handle, out var length);
            Throw();

            var bytes = new byte[length];
            Marshal.Copy(value, bytes, 0, length);
            return bytes;
        }

        /// <summary>
        /// If an error has occurred, throw it.  
        /// </summary>
        void Throw()
        {
            Throw(msg => new Exception(msg));
        }

        /// <summary>
        /// If an error has occurred, throw it.  
        /// </summary>
        void Throw(Func<string, Exception> exception)
        {
            Native.leveldb_iter_get_error(Handle, out var error);
            if (error != IntPtr.Zero) throw exception(Marshal.PtrToStringAnsi(error));
        }

        protected override void FreeUnManagedObjects()
        {
            Native.leveldb_iter_destroy(Handle);
        }
    }
}
