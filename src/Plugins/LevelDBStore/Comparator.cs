// Copyright (C) 2015-2024 The Neo Project.
//
// Comparator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using LevelDB.NativePointer;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LevelDB
{
    public class Comparator : LevelDBHandle
    {
        private sealed class Inner : IDisposable
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate void Destructor(IntPtr gCHandleThis);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate int Compare(IntPtr gCHandleThisg,
                                         IntPtr data1, IntPtr size1,
                                         IntPtr data2, IntPtr size2);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate IntPtr Name(IntPtr gCHandleThis);


            private static readonly Destructor s_destructor
                = (gCHandleThis) =>
                      {
                          var h = GCHandle.FromIntPtr(gCHandleThis);
                          var @this = h.Target as Inner;

                          @this.Dispose();

                          // TODO: At the point 'Free' is entered, this delegate may become eligible to be GC'd.
                          // TODO:  Need to look whether GC might run between then, and when this delegate returns.
                          h.Free();
                      };

            private static readonly Compare s_compare =
                (gCHandleThis, data1, size1, data2, size2) =>
                    {
                        var @this = GCHandle.FromIntPtr(gCHandleThis).Target as Inner;
                        return @this._cmp(new NativeArray { _baseAddr = data1, _byteLength = size1 },
                                        new NativeArray { _baseAddr = data2, _byteLength = size2 });
                    };

            private static readonly Name s_nameAccessor =
                (gCHandleThis) =>
                    {
                        var @this = GCHandle.FromIntPtr(gCHandleThis).Target as Inner;
                        return @this.NameValue;
                    };

            private Func<NativeArray, NativeArray, int> _cmp;
            private GCHandle _namePinned;

            public IntPtr Init(string name, Func<NativeArray, NativeArray, int> cmp)
            {
                // TODO: Complete member initialization
                _cmp = cmp;

                _namePinned = GCHandle.Alloc(
                    Encoding.ASCII.GetBytes(name),
                    GCHandleType.Pinned);

                var thisHandle = GCHandle.Alloc(this);

                var chandle = LevelDBInterop.leveldb_comparator_create(
                    GCHandle.ToIntPtr(thisHandle),
                    Marshal.GetFunctionPointerForDelegate(s_destructor),
                    Marshal.GetFunctionPointerForDelegate(s_compare),
                    Marshal.GetFunctionPointerForDelegate(s_nameAccessor)
                    );

                if (chandle == default)
                    thisHandle.Free();
                return chandle;
            }

            private unsafe IntPtr NameValue
            {
                get
                {
                    // TODO: this is probably not the most effective way to get a pinned string
                    var s = ((byte[])_namePinned.Target);
                    fixed (byte* p = s)
                    {
                        // Note: pinning the GCHandle ensures this value should remain stable 
                        // Note:  outside of the 'fixed' block.
                        return (IntPtr)p;
                    }
                }
            }

            public void Dispose()
            {
                if (_namePinned.IsAllocated)
                    _namePinned.Free();
            }
        }

        private Comparator(string name, Func<NativeArray, NativeArray, int> cmp)
        {
            var inner = new Inner();
            try
            {
                Handle = inner.Init(name, cmp);
            }
            finally
            {
                if (Handle == default)
                    inner.Dispose();
            }
        }

        public static Comparator Create(string name, Func<NativeArray, NativeArray, int> cmp)
        {
            return new Comparator(name, cmp);
        }
        public static Comparator Create(string name, IComparer<NativeArray> cmp)
        {
            return new Comparator(name, (a, b) => cmp.Compare(a, b));
        }

        protected override void FreeUnManagedObjects()
        {
            if (Handle != default)
            {
                // indirectly invoked CleanupInner
                LevelDBInterop.leveldb_comparator_destroy(Handle);
            }
        }
    }
}
