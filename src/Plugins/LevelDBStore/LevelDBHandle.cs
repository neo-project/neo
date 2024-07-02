// Copyright (C) 2015-2024 The Neo Project.
//
// LevelDBHandle.cs file belongs to the neo project and is free
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
    /// Base class for all LevelDB objects
    /// Implement IDisposable as prescribed by http://msdn.microsoft.com/en-us/library/b1yfkh5e.aspx by overriding the two additional virtual methods
    /// </summary>
    public abstract class LevelDBHandle : IDisposable
    {
        public IntPtr Handle { protected set; get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void FreeManagedObjects()
        {
        }

        protected virtual void FreeUnManagedObjects()
        {
        }

        bool _disposed = false;
        void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    FreeManagedObjects();
                }
                if (Handle != IntPtr.Zero)
                {
                    FreeUnManagedObjects();
                    Handle = IntPtr.Zero;
                }
                _disposed = true;
            }
        }

        ~LevelDBHandle()
        {
            Dispose(false);
        }
    }
}
