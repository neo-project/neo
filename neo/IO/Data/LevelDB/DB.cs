using System;

namespace Neo.IO.Data.LevelDB
{
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

        public void Delete(WriteOptions options, Slice key)
        {
            IntPtr error;
            Native.leveldb_delete(handle, options.handle, key.buffer, (UIntPtr)key.buffer.Length, out error);
            NativeHelper.CheckError(error);
        }

        public Slice Get(ReadOptions options, Slice key)
        {
            UIntPtr length;
            IntPtr error;
            IntPtr value = Native.leveldb_get(handle, options.handle, key.buffer, (UIntPtr)key.buffer.Length, out length, out error);
            try
            {
                NativeHelper.CheckError(error);
                if (value == IntPtr.Zero)
                    throw new LevelDBException("not found");
                return new Slice(value, length);
            }
            finally
            {
                if (value != IntPtr.Zero) Native.leveldb_free(value);
            }
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
            IntPtr error;
            IntPtr handle = Native.leveldb_open(options.handle, name, out error);
            NativeHelper.CheckError(error);
            return new DB(handle);
        }

        public void Put(WriteOptions options, Slice key, Slice value)
        {
            IntPtr error;
            Native.leveldb_put(handle, options.handle, key.buffer, (UIntPtr)key.buffer.Length, value.buffer, (UIntPtr)value.buffer.Length, out error);
            NativeHelper.CheckError(error);
        }

        public bool TryGet(ReadOptions options, Slice key, out Slice value)
        {
            UIntPtr length;
            IntPtr error;
            IntPtr v = Native.leveldb_get(handle, options.handle, key.buffer, (UIntPtr)key.buffer.Length, out length, out error);
            if (error != IntPtr.Zero)
            {
                Native.leveldb_free(error);
                value = default(Slice);
                return false;
            }
            if (v == IntPtr.Zero)
            {
                value = default(Slice);
                return false;
            }
            value = new Slice(v, length);
            Native.leveldb_free(v);
            return true;
        }

        public void Write(WriteOptions options, WriteBatch write_batch)
        {
            // There's a bug in .Net Core.
            // When calling DB.Write(), it will throw LevelDBException sometimes.
            // But when you try to catch the exception, the bug disappears.
            // We shall remove the "try...catch" clause when Microsoft fix the bug.
            byte retry = 0;
            while (true)
            {
                try
                {
                    IntPtr error;
                    Native.leveldb_write(handle, options.handle, write_batch.handle, out error);
                    NativeHelper.CheckError(error);
                    break;
                }
                catch (LevelDBException ex)
                {
                    if (++retry >= 4) throw;
                    System.IO.File.AppendAllText("leveldb.log", ex.Message + "\r\n");
                }
            }
        }
    }
}
