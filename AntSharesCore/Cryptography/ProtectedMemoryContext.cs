using System;
using System.Security.Cryptography;

namespace AntShares.Cryptography
{
    internal class ProtectedMemoryContext : IDisposable
    {
        private byte[] memory;
        private MemoryProtectionScope scope;

        public ProtectedMemoryContext(byte[] memory, MemoryProtectionScope scope)
        {
            this.memory = memory;
            this.scope = scope;
            ProtectedMemory.Unprotect(memory, scope);
        }

        void IDisposable.Dispose()
        {
            ProtectedMemory.Protect(memory, scope);
        }
    }
}
