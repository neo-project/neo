using System;

namespace Neo.IO.Data.LevelDB
{
    public class Snapshot : IDisposable
    {
        private readonly IntPtr db;

        internal IntPtr Handle { get; private set; }

        internal Snapshot(IntPtr db)
        {
            this.db = db;
            Handle = Native.leveldb_create_snapshot(db);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                Native.leveldb_release_snapshot(db, Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
