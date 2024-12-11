// Copyright (C) 2015-2024 The Neo Project.
//
// Native.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Neo.IO.Storage.LevelDB
{
    public enum CompressionType : byte
    {
        NoCompression = 0x0,
        SnappyCompression = 0x1
    }

    public static class Native
    {
        #region Logger
        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_logger_create(nint /* Action<string> */ logger);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_logger_destroy(nint /* logger*/ option);
        #endregion

        #region DB
        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_open(nint /* Options*/ options, string name, out nint error);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_close(nint /*DB */ db);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_put(nint /* DB */ db, nint /* WriteOptions*/ options, byte[] key, UIntPtr keylen, byte[] val, UIntPtr vallen, out nint errptr);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_delete(nint /* DB */ db, nint /* WriteOptions*/ options, byte[] key, UIntPtr keylen, out nint errptr);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_write(nint /* DB */ db, nint /* WriteOptions*/ options, nint /* WriteBatch */ batch, out nint errptr);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_get(nint /* DB */ db, nint /* ReadOptions*/ options, byte[] key, UIntPtr keylen, out UIntPtr vallen, out nint errptr);

        //[DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //static extern void leveldb_approximate_sizes(nint /* DB */ db, int num_ranges, byte[] range_start_key, long range_start_key_len, byte[] range_limit_key, long range_limit_key_len, out long sizes);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_create_iterator(nint /* DB */ db, nint /* ReadOption */ options);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_create_snapshot(nint /* DB */ db);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_release_snapshot(nint /* DB */ db, nint /* SnapShot*/ snapshot);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_property_value(nint /* DB */ db, string propname);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_repair_db(nint /* Options*/ options, string name, out nint error);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_destroy_db(nint /* Options*/ options, string name, out nint error);

        #region extensions

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_free(nint /* void */ ptr);

        #endregion


        #endregion

        #region Env
        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_create_default_env();

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_env_destroy(nint /*Env*/ cache);
        #endregion

        #region Iterator
        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_iter_destroy(nint /*Iterator*/ iterator);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool leveldb_iter_valid(nint /*Iterator*/ iterator);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_iter_seek_to_first(nint /*Iterator*/ iterator);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_iter_seek_to_last(nint /*Iterator*/ iterator);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_iter_seek(nint /*Iterator*/ iterator, byte[] key, UIntPtr length);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_iter_next(nint /*Iterator*/ iterator);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_iter_prev(nint /*Iterator*/ iterator);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_iter_key(nint /*Iterator*/ iterator, out UIntPtr length);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_iter_value(nint /*Iterator*/ iterator, out UIntPtr length);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_iter_get_error(nint /*Iterator*/ iterator, out nint error);
        #endregion

        #region Options
        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_options_create();

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_destroy(nint /*Options*/ options);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_create_if_missing(nint /*Options*/ options, [MarshalAs(UnmanagedType.U1)] bool o);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_error_if_exists(nint /*Options*/ options, [MarshalAs(UnmanagedType.U1)] bool o);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_info_log(nint /*Options*/ options, nint /* Logger */ logger);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_paranoid_checks(nint /*Options*/ options, [MarshalAs(UnmanagedType.U1)] bool o);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_env(nint /*Options*/ options, nint /*Env*/ env);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_write_buffer_size(nint /*Options*/ options, UIntPtr size);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_max_open_files(nint /*Options*/ options, int max);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_cache(nint /*Options*/ options, nint /*Cache*/ cache);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_block_size(nint /*Options*/ options, UIntPtr size);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_block_restart_interval(nint /*Options*/ options, int interval);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_compression(nint /*Options*/ options, CompressionType level);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_comparator(nint /*Options*/ options, nint /*Comparator*/ comparer);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_options_set_filter_policy(nint /*Options*/ options, nint /*FilterPolicy*/ policy);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_filterpolicy_create_bloom(int bits_per_key);
        #endregion

        #region ReadOptions
        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_readoptions_create();

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_readoptions_destroy(nint /*ReadOptions*/ options);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_readoptions_set_verify_checksums(nint /*ReadOptions*/ options, [MarshalAs(UnmanagedType.U1)] bool o);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_readoptions_set_fill_cache(nint /*ReadOptions*/ options, [MarshalAs(UnmanagedType.U1)] bool o);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_readoptions_set_snapshot(nint /*ReadOptions*/ options, nint /*SnapShot*/ snapshot);
        #endregion

        #region WriteBatch
        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_writebatch_create();

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_writebatch_destroy(nint /* WriteBatch */ batch);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_writebatch_clear(nint /* WriteBatch */ batch);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_writebatch_put(nint /* WriteBatch */ batch, byte[] key, UIntPtr keylen, byte[] val, UIntPtr vallen);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_writebatch_delete(nint /* WriteBatch */ batch, byte[] key, UIntPtr keylen);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_writebatch_iterate(nint /* WriteBatch */ batch, object state, Action<object, byte[], int, byte[], int> put, Action<object, byte[], int> deleted);
        #endregion

        #region WriteOptions
        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_writeoptions_create();

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_writeoptions_destroy(nint /*WriteOptions*/ options);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_writeoptions_set_sync(nint /*WriteOptions*/ options, [MarshalAs(UnmanagedType.U1)] bool o);
        #endregion

        #region Cache
        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint leveldb_cache_create_lru(int capacity);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_cache_destroy(nint /*Cache*/ cache);
        #endregion

        #region Comparator

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint /* leveldb_comparator_t* */
            leveldb_comparator_create(
            nint /* void* */ state,
            nint /* void (*)(void*) */ destructor,
            nint
                /* int (*compare)(void*,
                                  const char* a, size_t alen,
                                  const char* b, size_t blen) */
                compare,
            nint /* const char* (*)(void*) */ name);

        [DllImport("libleveldb", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void leveldb_comparator_destroy(nint /* leveldb_comparator_t* */ cmp);

        #endregion
    }

    internal static class NativeHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckError(nint error)
        {
            if (error != nint.Zero)
            {
                string message = Marshal.PtrToStringAnsi(error);
                Native.leveldb_free(error);
                throw new LevelDBException(message);
            }
        }
    }
}
