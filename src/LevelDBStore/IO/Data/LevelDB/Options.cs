// Copyright (C) 2015-2024 The Neo Project.
//
// Options.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.IO.Data.LevelDB
{
    public class Options
    {
        public static readonly Options Default = new Options();
        internal readonly IntPtr handle = Native.leveldb_options_create();

        public bool CreateIfMissing
        {
            set
            {
                Native.leveldb_options_set_create_if_missing(handle, value);
            }
        }

        public bool ErrorIfExists
        {
            set
            {
                Native.leveldb_options_set_error_if_exists(handle, value);
            }
        }

        public bool ParanoidChecks
        {
            set
            {
                Native.leveldb_options_set_paranoid_checks(handle, value);
            }
        }

        public int WriteBufferSize
        {
            set
            {
                Native.leveldb_options_set_write_buffer_size(handle, (UIntPtr)value);
            }
        }

        public int MaxOpenFiles
        {
            set
            {
                Native.leveldb_options_set_max_open_files(handle, value);
            }
        }

        public int BlockSize
        {
            set
            {
                Native.leveldb_options_set_block_size(handle, (UIntPtr)value);
            }
        }

        public int BlockRestartInterval
        {
            set
            {
                Native.leveldb_options_set_block_restart_interval(handle, value);
            }
        }

        public CompressionType Compression
        {
            set
            {
                Native.leveldb_options_set_compression(handle, value);
            }
        }

        public IntPtr FilterPolicy
        {
            set
            {
                Native.leveldb_options_set_filter_policy(handle, value);
            }
        }

        ~Options()
        {
            Native.leveldb_options_destroy(handle);
        }
    }
}
