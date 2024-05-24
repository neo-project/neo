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
using System.Text;
namespace LevelDB
{
    /// <summary>
    /// An iterator yields a sequence of key/value pairs from a database.
    /// </summary>
    public class Iterator : LevelDBHandle
    {
        private readonly Encoding _encoding;

        internal Iterator(IntPtr Handle, Encoding encoding)
        {
            _encoding = encoding;
            this.Handle = Handle;
        }

        /// <summary>
        /// An iterator is either positioned at a key/value pair, or
        /// not valid.  
        /// </summary>
        /// <returns>This method returns true iff the iterator is valid.</returns>
        public bool IsValid()
        {
            return (int)LevelDBInterop.leveldb_iter_valid(Handle) != 0;
        }

        /// <summary>
        /// Position at the first key in the source.  
        /// The iterator is Valid() after this call iff the source is not empty.
        /// </summary>
        public void SeekToFirst()
        {
            LevelDBInterop.leveldb_iter_seek_to_first(Handle);
            Throw();
        }

        /// <summary>
        /// Position at the last key in the source.  
        /// The iterator is Valid() after this call iff the source is not empty.
        /// </summary>
        public void SeekToLast()
        {
            LevelDBInterop.leveldb_iter_seek_to_last(Handle);
            Throw();
        }

        /// <summary>
        /// Position at the first key in the source that at or past target
        /// The iterator is Valid() after this call iff the source contains
        /// an entry that comes at or past target.
        /// </summary>
        public void Seek(byte[] key)
        {
            LevelDBInterop.leveldb_iter_seek(Handle, key, key.Length);
            Throw();
        }

        /// <summary>
        /// Position at the first key in the source that at or past target
        /// The iterator is Valid() after this call iff the source contains
        /// an entry that comes at or past target.
        /// </summary>
        public void Seek(string key)
        {
            Seek(_encoding.GetBytes(key));
        }

        /// <summary>
        /// Position at the first key in the source that at or past target
        /// The iterator is Valid() after this call iff the source contains
        /// an entry that comes at or past target.
        /// </summary>
        public void Seek(int key)
        {
            LevelDBInterop.leveldb_iter_seek(Handle, ref key, 4);
            Throw();
        }

        /// <summary>
        /// Moves to the next entry in the source.  
        /// After this call, Valid() is true iff the iterator was not positioned at the last entry in the source.
        /// REQUIRES: Valid()
        /// </summary>
        public void Next()
        {
            LevelDBInterop.leveldb_iter_next(Handle);
            Throw();
        }

        /// <summary>
        /// Moves to the previous entry in the source.  
        /// After this call, Valid() is true iff the iterator was not positioned at the first entry in source.
        /// REQUIRES: Valid()
        /// </summary>
        public void Prev()
        {
            LevelDBInterop.leveldb_iter_prev(Handle);
            Throw();
        }


        /// <summary>
        /// Return the key for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public int KeyAsInt()
        {
            int length;
            var key = LevelDBInterop.leveldb_iter_key(Handle, out length);
            Throw();

            if (length != 4) throw new Exception("Key is not an integer");

            return Marshal.ReadInt32(key);
        }

        /// <summary>
        /// Return the key for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public string KeyAsString()
        {
            return _encoding.GetString(Key());
        }

        /// <summary>
        /// Return the key for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public byte[] Key()
        {
            int length;
            var key = LevelDBInterop.leveldb_iter_key(Handle, out length);
            Throw();

            var bytes = new byte[length];
            Marshal.Copy(key, bytes, 0, length);
            return bytes;
        }

        /// <summary>
        /// Return the value for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public int[] ValueAsInts()
        {
            int length;
            var value = LevelDBInterop.leveldb_iter_value(Handle, out length);
            Throw();

            var bytes = new int[length / 4];
            Marshal.Copy(value, bytes, 0, length / 4);
            return bytes;
        }

        /// <summary>
        /// Return the value for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public string ValueAsString()
        {
            return _encoding.GetString(Value());
        }

        /// <summary>
        /// Return the value for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public byte[] Value()
        {
            int length;
            var value = LevelDBInterop.leveldb_iter_value(Handle, out length);
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
            IntPtr error;
            LevelDBInterop.leveldb_iter_get_error(Handle, out error);
            if (error != IntPtr.Zero) throw exception(Marshal.PtrToStringAnsi(error));
        }

        protected override void FreeUnManagedObjects()
        {
            LevelDBInterop.leveldb_iter_destroy(Handle);
        }
    }
}
