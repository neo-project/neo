using System;

namespace Neo.IO.Data.LevelDB
{
    public class WriteBatch : IDisposable
    {
        /// <summary>
        /// Return true if haven't got valid handle
        /// </summary>
        public bool IsDisposed => Handle == IntPtr.Zero;

        /// <summary>
        /// Handle
        /// </summary>
        internal IntPtr Handle { get; private set; }

        public WriteBatch()
        {
            Handle = Native.leveldb_writebatch_create();
        }

        public void Clear()
        {
            Native.leveldb_writebatch_clear(Handle);
        }

        public void Delete(Slice key)
        {
            Native.leveldb_writebatch_delete(Handle, key.buffer, (UIntPtr)key.buffer.Length);
        }

        public void Put(Slice key, Slice value)
        {
            Native.leveldb_writebatch_put(Handle, key.buffer, (UIntPtr)key.buffer.Length, value.buffer, (UIntPtr)value.buffer.Length);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                Native.leveldb_writebatch_destroy(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
