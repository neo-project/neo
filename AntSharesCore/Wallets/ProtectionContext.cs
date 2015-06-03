using System;
using System.Security.Cryptography;

namespace AntShares.Wallets
{
    internal class ProtectionContext : IDisposable
    {
        private byte[] data;

        public ProtectionContext(byte[] data)
        {
            this.data = data;
            ProtectedMemory.Unprotect(data, MemoryProtectionScope.SameProcess);
        }

        public void Dispose()
        {
            ProtectedMemory.Protect(data, MemoryProtectionScope.SameProcess);
        }
    }
}
