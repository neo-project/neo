// Copyright (C) 2015-2025 The Neo Project.
//
// UnmanagedBuffer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Neo.Build.Core.Security
{
    public sealed class UnmanagedBuffer : SafeBuffer
    {
        // A local copy of byte length to be able to access it in ReleaseHandle without the risk of throwing exceptions
        private int _byteLength;

        private UnmanagedBuffer() : base(true) { }

        public static UnmanagedBuffer Allocate(int byteLength)
        {
            Debug.Assert(byteLength >= 0);
            var buffer = new UnmanagedBuffer();
            buffer.SetHandle(Marshal.AllocHGlobal(byteLength));
            buffer.Initialize((ulong)byteLength);
            buffer._byteLength = byteLength;
            return buffer;
        }

        internal static unsafe void Copy(UnmanagedBuffer source, UnmanagedBuffer destination, ulong bytesLength)
        {
            if (bytesLength == 0)
                return;

            byte* srcPtr = null, dstPtr = null;
            try
            {
                source.AcquirePointer(ref srcPtr);
                destination.AcquirePointer(ref dstPtr);
                Buffer.MemoryCopy(srcPtr, dstPtr, destination.ByteLength, bytesLength);
            }
            finally
            {
                if (dstPtr != null)
                    destination.ReleasePointer();

                if (srcPtr != null)
                    source.ReleasePointer();
            }
        }

        protected override unsafe bool ReleaseHandle()
        {
            new Span<byte>((void*)handle, _byteLength).Clear();
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
