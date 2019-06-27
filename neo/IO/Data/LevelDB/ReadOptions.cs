using System;

namespace Neo.IO.Data.LevelDB
{
    public class ReadOptions : IDisposable
    {
        public static readonly ReadOptions Default = new ReadOptions();
        
        /// <summary>
        /// Return true if haven't got valid handle
        /// </summary>
        public bool IsDisposed => Handle == IntPtr.Zero;

        /// <summary>
        /// Handle
        /// </summary>
        internal IntPtr Handle { get; private set; }

        public bool VerifyChecksums
        {
            set
            {
                Native.leveldb_readoptions_set_verify_checksums(Handle, value);
            }
        }

        public bool FillCache
        {
            set
            {
                Native.leveldb_readoptions_set_fill_cache(Handle, value);
            }
        }

        public Snapshot Snapshot
        {
            set
            {
                Native.leveldb_readoptions_set_snapshot(Handle, value.Handle);
            }
        }

        public ReadOptions()
        {
            Handle = Native.leveldb_readoptions_create();
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                Native.leveldb_options_destroy(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
