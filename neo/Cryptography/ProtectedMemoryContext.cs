using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    internal class ProtectedMemoryContext : IDisposable
    {
        private static Dictionary<byte[], byte> counts = new Dictionary<byte[], byte>();
        private byte[] memory;
        private DataProtectionScope scope;
        static byte[] s_additionalEntropy = { 9, 8, 7, 6, 5 };

        public ProtectedMemoryContext(byte[] memory, DataProtectionScope scope)
        {
            this.memory = memory;
            this.scope = scope;
            if (counts.ContainsKey(memory))
            {
                counts[memory]++;
            }
            else
            {
                counts.Add(memory, 1);
                Unprotect(memory, scope);
            }
        }

        void IDisposable.Dispose()
        {
            if (--counts[memory] == 0)
            {
                counts.Remove(memory);
                Protect(memory, scope);
            }
        }

        public static byte[] Protect(byte[] data, DataProtectionScope scope)
        {
            try
            {
                // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted
                //  only by the same current user.
                return ProtectedData.Protect(data, s_additionalEntropy, scope);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not encrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        public static byte[] Unprotect(byte[] data, DataProtectionScope scope)
        {
            try
            {
                //Decrypt the data using DataProtectionScope.CurrentUser.
                return ProtectedData.Unprotect(data, s_additionalEntropy, scope);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not decrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
        }
    }
}