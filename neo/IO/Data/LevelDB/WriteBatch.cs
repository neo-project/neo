using System;

namespace Neo.IO.Data.LevelDB
{
    public class WriteBatch
    {
        internal readonly IntPtr handle = Native.leveldb_writebatch_create();

        ~WriteBatch()
        {
            Native.leveldb_writebatch_destroy(handle);
        }

        public virtual void Clear()
        {
            Native.leveldb_writebatch_clear(handle);
        }

        public virtual void Delete(Slice key)
        {
            Native.leveldb_writebatch_delete(handle, key.buffer, (UIntPtr)key.buffer.Length);
        }

        public virtual void Put(Slice key, Slice value)
        {
            Native.leveldb_writebatch_put(handle, key.buffer, (UIntPtr)key.buffer.Length, value.buffer, (UIntPtr)value.buffer.Length);
        }
    }
}
