// Copyright (C) 2015-2024 The Neo Project.
//
// SnapShot.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace LevelDB
{
    /// <summary>
    /// A Snapshot is an immutable object and can therefore be safely
    /// accessed from multiple threads without any external synchronization.
    /// </summary>
    public class SnapShot : LevelDBHandle
    {
        // pointer to parent so that we can call ReleaseSnapshot(this) when disposed
        public WeakReference Parent;  // as DB

        internal SnapShot(IntPtr handle, DB parent)
        {
            this.Handle = handle;
            Parent = new WeakReference(parent);
        }

        internal SnapShot(IntPtr Handle)
        {
            this.Handle = Handle;
            Parent = new WeakReference(null);
        }

        protected override void FreeUnManagedObjects()
        {
            if (Parent.IsAlive)
            {
                var parent = Parent.Target as DB;
                if (parent != null) LevelDBInterop.leveldb_release_snapshot(parent.Handle, Handle);
            }
        }
    }
}
