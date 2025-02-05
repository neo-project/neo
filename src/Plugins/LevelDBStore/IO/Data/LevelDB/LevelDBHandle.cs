// Copyright (C) 2015-2025 The Neo Project.
//
// LevelDBHandle.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.IO.Storage.LevelDB
{
    /// <summary>
    /// Base class for all LevelDB objects
    /// </summary>
    public abstract class LevelDBHandle(nint handle) : IDisposable
    {
        private bool _disposed = false;

        public nint Handle { get; private set; } = handle;

        /// <summary>
        /// Return true if haven't got valid handle
        /// </summary>
        public bool IsDisposed => _disposed || Handle == IntPtr.Zero;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void FreeUnManagedObjects();

        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (Handle != nint.Zero)
                {
                    FreeUnManagedObjects();
                    Handle = nint.Zero;
                }
            }
        }

        ~LevelDBHandle()
        {
            Dispose(false);
        }
    }
}
