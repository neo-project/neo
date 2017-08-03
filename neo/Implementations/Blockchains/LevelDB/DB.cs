using System;
using Neo.Implementations.Blockchains.Utilities;

namespace Neo.Implementations.Blockchains.LevelDB
{
    internal class DB : AbstractDB
    {
        private IntPtr handle;

        private DB(IntPtr handle)
        {
            this.handle = handle;
        }

        public override void Dispose()
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
            Native.leveldb_delete(handle, getHandle(options), key.buffer, (UIntPtr)key.buffer.Length, out error);
            NativeHelper.CheckError(error);
        }

        public override Slice Get(AbstractReadOptions options, Slice key)
        {
            UIntPtr length;
            IntPtr error;
            IntPtr value = Native.leveldb_get(handle, getHandle(options), key.buffer, (UIntPtr)key.buffer.Length, out length, out error);
            try
            {
                NativeHelper.CheckError(error);
                if (value == IntPtr.Zero)
                    throw new DBException("not found");
                return new Slice(value, length);
            }
            finally
            {
                if (value != IntPtr.Zero) Native.leveldb_free(value);
            }
        }

        public override AbstractSnapshot GetSnapshot()
        {
            return new Snapshot(handle);
        }

        public override AbstractIterator NewIterator(AbstractReadOptions options)
        {
            return new Iterator(Native.leveldb_create_iterator(handle, getHandle(options)));
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

        public override void Put(AbstractWriteOptions options, Slice key, Slice value)
        {
            IntPtr error;
            Native.leveldb_put(handle, getHandle(options), key.buffer, (UIntPtr)key.buffer.Length, value.buffer, (UIntPtr)value.buffer.Length, out error);
            NativeHelper.CheckError(error);
        }

		private IntPtr getHandle(AbstractWriteBatch write_batch)
		{
			return ((WriteBatch)write_batch).handle;
		}

		private IntPtr getHandle(AbstractReadOptions options)
		{
			return ((ReadOptions)options).handle;
		}

		private IntPtr getHandle(AbstractWriteOptions options)
		{
			return ((WriteOptions)options).handle;
		}
		
        public override bool TryGet(AbstractReadOptions options, Slice key, out Slice value)
        {
            UIntPtr length;
            IntPtr error;
            IntPtr v = Native.leveldb_get(handle, getHandle(options), key.buffer, (UIntPtr)key.buffer.Length, out length, out error);
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

        public override void Write(AbstractWriteOptions options, AbstractWriteBatch write_batch)
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
                    Native.leveldb_write(handle, getHandle(options), getHandle(write_batch), out error);
                    NativeHelper.CheckError(error);
                    break;
                }
                catch (DBException ex)
                {
                    if (++retry >= 4) throw;
                    System.IO.File.AppendAllText("leveldb.log", ex.Message + "\r\n");
                }
            }
        }
    }
}
