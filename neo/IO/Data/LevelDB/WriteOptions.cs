using System;

namespace Neo.IO.Data.LevelDB
{
    public class WriteOptions
    {
        public static readonly WriteOptions Default = new WriteOptions();

        /// <summary>
        /// Return true if haven't got valid handle
        /// </summary>
        public bool IsDisposed => Handle == IntPtr.Zero;

        /// <summary>
        /// Handle
        /// </summary>
        internal IntPtr Handle { get; private set; }

        public WriteOptions()
        {
            Handle = Native.leveldb_writeoptions_create();
        }

        public bool Sync
        {
            set
            {
                Native.leveldb_writeoptions_set_sync(Handle, value);
            }
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                Native.leveldb_writeoptions_destroy(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
