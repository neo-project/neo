// Copyright (C) 2015-2024 The Neo Project.
//
// Logger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.InteropServices;

namespace LevelDB
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Log(string msg);

    public class Logger : LevelDBHandle
    {
        public Logger(Log log)
        {
            var p = Marshal.GetFunctionPointerForDelegate(log);
            Handle = LevelDBInterop.leveldb_logger_create(p);
        }

        public static implicit operator Logger(Log log)
        {
            return new Logger(log);
        }

        protected override void FreeUnManagedObjects()
        {
            if (Handle != default(IntPtr))
                LevelDBInterop.leveldb_logger_destroy(Handle);
        }
    }
}
