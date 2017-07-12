using System;

namespace Neo.Implementations.Blockchains.LevelDB
{
    internal class WriteOptions
    {
        public static readonly WriteOptions Default = new WriteOptions();
        internal readonly IntPtr handle = Native.leveldb_writeoptions_create();

        public bool Sync
        {
            set
            {
                Native.leveldb_writeoptions_set_sync(handle, value);
            }
        }

        ~WriteOptions()
        {
            Native.leveldb_writeoptions_destroy(handle);
        }
    }
}
