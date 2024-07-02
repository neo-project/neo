// Copyright (C) 2015-2024 The Neo Project.
//
// PinnedSafeHandle.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using LevelDB.NativePointer;
using System;
using System.Runtime.InteropServices;

namespace LevelDB
{
    internal class PinnedSafeHandle<T> : SafeHandle
        where T : struct
    {
        private GCHandle pinnedRawData;

        public PinnedSafeHandle(T[] arr)
            : base(default(IntPtr), true)
        {
            pinnedRawData = GCHandle.Alloc(arr, GCHandleType.Pinned);

            // initialize handle last; ensure we only free initialized GCHandles.
            handle = pinnedRawData.AddrOfPinnedObject();
        }

        public Ptr<T> Ptr
        {
            get { return (Ptr<T>)handle; }
        }

        public override bool IsInvalid
        {
            get { return handle == default(IntPtr); }
        }

        protected override bool ReleaseHandle()
        {
            if (handle != default(IntPtr))
            {
                pinnedRawData.Free();
                handle = default(IntPtr);
            }
            return true;
        }
    }
}
