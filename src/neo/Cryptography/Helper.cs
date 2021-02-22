using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Neo.Cryptography
{
    public static class Helper
    {
        public enum CypherMode
        {
            Ecb,
            Cbc
        }
        internal static byte[] AESDecryptNoPadding(this byte[] data, byte[] key, CypherMode cypher, byte[] iv = null)
        {
            if (cypher == CypherMode.Cbc)
            {
                if (data == null || key == null || iv == null) throw new ArgumentNullException();
                if (data.Length % 16 != 0 || key.Length != 32 || iv.Length != 16) throw new ArgumentNullException();
            }
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Padding = PaddingMode.None;
                if (cypher == CypherMode.Ecb)
                    aes.Mode = CipherMode.ECB;
                else if (cypher == CypherMode.Cbc)
                    aes.IV = iv;
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        internal static byte[] AESEncryptNoPadding(this byte[] data, byte[] key, CypherMode cypher, byte[] iv = null)
        {
            if (cypher == CypherMode.Cbc)
            {
                if (data == null || key == null || iv == null) throw new ArgumentNullException();
                if (data.Length % 16 != 0 || key.Length != 32 || iv.Length != 16) throw new ArgumentNullException();
            }
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Padding = PaddingMode.None;
                if (cypher == CypherMode.Ecb)
                    aes.Mode = CipherMode.ECB;
                else if (cypher == CypherMode.Cbc)
                    aes.IV = iv;
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                }
            }
        }

        public static byte[] RIPEMD160(this byte[] value)
        {
            using var ripemd160 = new RIPEMD160Managed();
            return ripemd160.ComputeHash(value);
        }

        public static byte[] RIPEMD160(this ReadOnlySpan<byte> value)
        {
            byte[] source = value.ToArray();
            return source.RIPEMD160();
        }

        public static uint Murmur32(this byte[] value, uint seed)
        {
            using (Murmur3 murmur = new Murmur3(seed))
            {
                return BinaryPrimitives.ReadUInt32LittleEndian(murmur.ComputeHash(value));
            }
        }

        public static byte[] Sha256(this byte[] value)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(value);
        }

        public static byte[] Sha256(this byte[] value, int offset, int count)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(value, offset, count);
        }

        public static byte[] Sha256(this ReadOnlySpan<byte> value)
        {
            byte[] buffer = new byte[32];
            using var sha256 = SHA256.Create();
            sha256.TryComputeHash(value, buffer, out _);
            return buffer;
        }

        public static byte[] Sha256(this Span<byte> value)
        {
            return Sha256((ReadOnlySpan<byte>)value);
        }

        internal static bool Test(this BloomFilter filter, Transaction tx)
        {
            if (filter.Check(tx.Hash.ToArray())) return true;
            if (tx.Signers.Any(p => filter.Check(p.Account.ToArray())))
                return true;
            return false;
        }

        internal static byte[] ToAesKey(this string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] passwordHash = sha256.ComputeHash(passwordBytes);
                byte[] passwordHash2 = sha256.ComputeHash(passwordHash);
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
                Array.Clear(passwordHash, 0, passwordHash.Length);
                return passwordHash2;
            }
        }

        internal static byte[] ToAesKey(this SecureString password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] passwordBytes = password.ToArray();
                byte[] passwordHash = sha256.ComputeHash(passwordBytes);
                byte[] passwordHash2 = sha256.ComputeHash(passwordHash);
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
                Array.Clear(passwordHash, 0, passwordHash.Length);
                return passwordHash2;
            }
        }

        internal static byte[] ToArray(this SecureString s)
        {
            if (s == null)
                throw new NullReferenceException();
            if (s.Length == 0)
                return Array.Empty<byte>();
            List<byte> result = new List<byte>();
            IntPtr ptr = SecureStringMarshal.SecureStringToGlobalAllocAnsi(s);
            try
            {
                int i = 0;
                do
                {
                    byte b = Marshal.ReadByte(ptr, i++);
                    if (b == 0)
                        break;
                    result.Add(b);
                } while (true);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocAnsi(ptr);
            }
            return result.ToArray();
        }
    }
}
