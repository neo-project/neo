using System;
using Neo.Implementations.Blockchains.Utilities;

namespace Neo.Implementations.Blockchains.LevelDB
{
    internal class ReadOptions : AbstractReadOptions
    {
        public static readonly ReadOptions Default = new ReadOptions();
        internal readonly IntPtr handle = Native.leveldb_readoptions_create();

        public bool VerifyChecksums
        {
            set
            {
                Native.leveldb_readoptions_set_verify_checksums(handle, value);
            }
        }

        public override bool FillCache
        {
            set
            {
                Native.leveldb_readoptions_set_fill_cache(handle, value);
            }
        }

        public override AbstractSnapshot Snapshot
        {
            set
            {
                Native.leveldb_readoptions_set_snapshot(handle, ((Snapshot)value).handle);
            }
        }

        ~ReadOptions()
        {
            Native.leveldb_readoptions_destroy(handle);
        }
    }
}
