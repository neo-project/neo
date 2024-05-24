// Copyright (C) 2015-2024 The Neo Project.
//
// Env.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace LevelDB
{
    /// <summary>
    /// A default environment to access operating system functionality like 
    /// the filesystem etc of the current operating system.
    /// </summary>
    public class Env : LevelDBHandle
    {
        public Env()
        {
            Handle = LevelDBInterop.leveldb_create_default_env();
        }

        protected override void FreeUnManagedObjects()
        {
            LevelDBInterop.leveldb_env_destroy(Handle);
        }
    }
}
