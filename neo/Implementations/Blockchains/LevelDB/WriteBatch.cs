using System;
using Neo.Implementations.Blockchains.Utilities;

namespace Neo.Implementations.Blockchains.LevelDB
{
    internal class WriteBatch : AbstractWriteBatch
    {
        internal readonly IntPtr handle = Native.leveldb_writebatch_create();

        ~WriteBatch()
        {
            Native.leveldb_writebatch_destroy(handle);
        }

        public override void Clear()
        {
            Native.leveldb_writebatch_clear(handle);
        }

        public override void Delete(Slice key)
        {
            Native.leveldb_writebatch_delete(handle, key.buffer, (UIntPtr)key.buffer.Length);
        }

        public override void Put(Slice key, Slice value)
        {
            Native.leveldb_writebatch_put(handle, key.buffer, (UIntPtr)key.buffer.Length, value.buffer, (UIntPtr)value.buffer.Length);
        }
    }
}
