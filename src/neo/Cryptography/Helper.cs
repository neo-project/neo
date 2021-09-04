using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Wallets;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.Cryptography
{
    /// <summary>
    /// A helper class for cryptography
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Computes the hash value for the specified byte array using the ripemd160 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] RIPEMD160(this byte[] value)
        {
            using var ripemd160 = new RIPEMD160Managed();
            return ripemd160.ComputeHash(value);
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the ripemd160 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] RIPEMD160(this ReadOnlySpan<byte> value)
        {
            byte[] source = value.ToArray();
            return source.RIPEMD160();
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the murmur algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the murmur algorithm.</param>
        /// <returns>The computed hash code.</returns>
        public static uint Murmur32(this byte[] value, uint seed)
        {
            using Murmur32 murmur = new(seed);
            return BinaryPrimitives.ReadUInt32LittleEndian(murmur.ComputeHash(value));
        }

        /// <summary>
        /// Computes the 128-bit hash value for the specified byte array using the murmur algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the murmur algorithm.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] Murmur128(this byte[] value, uint seed)
        {
            using Murmur128 murmur = new(seed);
            return murmur.ComputeHash(value);
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the sha256 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] Sha256(this byte[] value)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(value);
        }

        /// <summary>
        /// Computes the hash value for the specified region of the specified byte array using the sha256 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="offset">The offset into the byte array from which to begin using data.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] Sha256(this byte[] value, int offset, int count)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(value, offset, count);
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the sha256 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] Sha256(this ReadOnlySpan<byte> value)
        {
            byte[] buffer = new byte[32];
            using var sha256 = SHA256.Create();
            sha256.TryComputeHash(value, buffer, out _);
            return buffer;
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the sha256 algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] Sha256(this Span<byte> value)
        {
            return Sha256((ReadOnlySpan<byte>)value);
        }

        public static string AES256Encrypt(this string plainData, string key, string nonce, string associatedData = null)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var nonceBytes = Encoding.UTF8.GetBytes(nonce);
            var associatedBytes = associatedData == null ? null : Encoding.UTF8.GetBytes(associatedData);
            var plainBytes = Encoding.UTF8.GetBytes(plainData);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[16];
            using var cipher = new AesGcm(keyBytes);
            cipher.Encrypt(nonceBytes, plainBytes, cipherBytes, tag, associatedBytes);
            var cipherWithTag = new byte[nonceBytes.Length + cipherBytes.Length + tag.Length];
            Buffer.BlockCopy(nonceBytes, 0, cipherWithTag, 0, nonceBytes.Length);
            Buffer.BlockCopy(cipherBytes, 0, cipherWithTag, nonceBytes.Length, cipherBytes.Length);
            Buffer.BlockCopy(tag, 0, cipherWithTag, nonceBytes.Length + cipherBytes.Length, tag.Length);
            return Convert.ToBase64String(cipherWithTag);
        }

        public static string AES256Decrypt(this string encryptedData, string key, string associatedData = null)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var associatedBytes = associatedData == null ? null : Encoding.UTF8.GetBytes(associatedData);
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var nonceBytes = encryptedBytes.Take(12).ToArray();
            var cipherBytes = encryptedBytes.Skip(12).Take(encryptedBytes.Length - 28).ToArray();
            var tag = encryptedBytes[^16..];
            var decryptedData = new byte[cipherBytes.Length];
            using var cipher = new AesGcm(keyBytes);
            cipher.Decrypt(nonceBytes, cipherBytes, tag, decryptedData, associatedBytes);
            return Encoding.UTF8.GetString(decryptedData);
        }

        public static byte[] ECEncrypt(byte[] message, ECPoint pubKey)
        {
            // P=kG,R=rG =>{R,M+rP}
            if (pubKey.IsInfinity) throw new ArgumentException();
            BigInteger r, rx;
            ECPoint R;
            var curve = pubKey.Curve;
            //r > N
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                do
                {
                    do
                    {
                        r = rng.NextBigInteger((int)curve.N.GetBitLength());
                    }
                    while (r.Sign == 0 || r.CompareTo(curve.N) >= 0);
                    R = ECPoint.Multiply(curve.G, r);
                    BigInteger x = R.X.Value;
                    rx = x.Mod(curve.N);
                }
                while (rx.Sign == 0);
            }
            byte[] RBar = R.EncodePoint(true);
            var EK = ECPoint.Multiply(pubKey, r).X.ToByteArray().Sha256(); // z = r * P = r* k * G
            Random random = new Random();
            byte[] Nonce = new byte[12];
            random.NextBytes(Nonce);
            var EM = Convert.ToBase64String(message).AES256Encrypt(Encoding.ASCII.GetString(EK), Encoding.ASCII.GetString(Nonce));
            return RBar.Concat(Convert.FromBase64String(EM)).ToArray();
        }

        public static byte[] ECDecrypt(byte[] cypher, KeyPair key)
        {
            // {R,M+rP}={rG, M+rP}=> M + rP - kR = M + r(kG) - k(rG) = M
            if (cypher is null || cypher.Length < 33)
                throw new ArgumentException();
            if (cypher[0] != 0x02 && cypher[0] != 0x03)
                throw new ArgumentException();
            if (key.PublicKey.IsInfinity) throw new ArgumentException();
            var RBar = cypher.Take(33).ToArray();
            var EM = cypher.Skip(33).ToArray();
            var R = ECPoint.FromBytes(RBar, key.PublicKey.Curve);
            var k = new BigInteger(key.PrivateKey.Reverse().Concat(new byte[1]).ToArray());
            var z = ECPoint.Multiply(R, k).X; // z = k * R = k * r * G
            var EK = z.ToByteArray().Sha256();
            var M = Convert.ToBase64String(EM).AES256Decrypt(Encoding.ASCII.GetString(EK));
            return Convert.FromBase64String(M);
        }

        internal static BigInteger NextBigInteger(this RandomNumberGenerator rng, int sizeInBits)
        {
            if (sizeInBits < 0)
                throw new ArgumentException("sizeInBits must be non-negative");
            if (sizeInBits == 0)
                return 0;
            byte[] b = new byte[sizeInBits / 8 + 1];
            rng.GetBytes(b);
            if (sizeInBits % 8 == 0)
                b[b.Length - 1] = 0;
            else
                b[b.Length - 1] &= (byte)((1 << sizeInBits % 8) - 1);
            return new BigInteger(b);
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
            using SHA256 sha256 = SHA256.Create();
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] passwordHash = sha256.ComputeHash(passwordBytes);
            byte[] passwordHash2 = sha256.ComputeHash(passwordHash);
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
            Array.Clear(passwordHash, 0, passwordHash.Length);
            return passwordHash2;
        }

        internal static byte[] ToAesKey(this SecureString password)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] passwordBytes = password.ToArray();
            byte[] passwordHash = sha256.ComputeHash(passwordBytes);
            byte[] passwordHash2 = sha256.ComputeHash(passwordHash);
            Array.Clear(passwordBytes, 0, passwordBytes.Length);
            Array.Clear(passwordHash, 0, passwordHash.Length);
            return passwordHash2;
        }

        internal static byte[] ToArray(this SecureString s)
        {
            if (s == null)
                throw new NullReferenceException();
            if (s.Length == 0)
                return Array.Empty<byte>();
            List<byte> result = new();
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
