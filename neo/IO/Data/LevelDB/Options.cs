using System;

namespace Neo.IO.Data.LevelDB
{
    public class Options : IDisposable
    {
        public static readonly Options Default = new Options() { CreateIfMissing = true };

        /// <summary>
        /// Return true if haven't got valid handle
        /// </summary>
        public bool IsDisposed => Handle == IntPtr.Zero;

        /// <summary>
        /// Handle
        /// </summary>
        internal IntPtr Handle { get; private set; }

        public Options()
        {
            Handle = Native.leveldb_options_create();
        }

        public bool CreateIfMissing
        {
            set
            {
                Native.leveldb_options_set_create_if_missing(Handle, value);
            }
        }

        public bool ErrorIfExists
        {
            set
            {
                Native.leveldb_options_set_error_if_exists(Handle, value);
            }
        }

        public bool ParanoidChecks
        {
            set
            {
                Native.leveldb_options_set_paranoid_checks(Handle, value);
            }
        }

        public int WriteBufferSize
        {
            set
            {
                Native.leveldb_options_set_write_buffer_size(Handle, (UIntPtr)value);
            }
        }

        public int MaxOpenFiles
        {
            set
            {
                Native.leveldb_options_set_max_open_files(Handle, value);
            }
        }

        public int BlockSize
        {
            set
            {
                Native.leveldb_options_set_block_size(Handle, (UIntPtr)value);
            }
        }

        public int BlockRestartInterval
        {
            set
            {
                Native.leveldb_options_set_block_restart_interval(Handle, value);
            }
        }

        public CompressionType Compression
        {
            set
            {
                Native.leveldb_options_set_compression(Handle, value);
            }
        }

        public IntPtr FilterPolicy
        {
            set
            {
                Native.leveldb_options_set_filter_policy(Handle, value);
            }
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
