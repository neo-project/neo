using System;
using Neo.Implementations.Blockchains.Utilities;

namespace Neo.Implementations.Blockchains.LevelDB
{
    internal class Snapshot : AbstractSnapshot
    {
        internal IntPtr db, handle;

        internal Snapshot(IntPtr db)
        {
            this.db = db;
            this.handle = Native.leveldb_create_snapshot(db);
        }

        public override void Dispose()
        {
            if (handle != IntPtr.Zero)
            {
                Native.leveldb_release_snapshot(db, handle);
                handle = IntPtr.Zero;
            }
        }
    }
}
