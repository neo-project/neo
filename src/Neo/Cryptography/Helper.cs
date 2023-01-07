// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Wallets;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Buffers.Binary;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static Neo.Helper;
using ECPoint = Neo.Cryptography.ECC.ECPoint;

namespace Neo.Cryptography
{
    /// <summary>
    /// A helper class for cryptography
    /// </summary>
    public static class Helper
    {
        private static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
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
        /// Computes the hash value for the specified byte array using the murmur algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the murmur algorithm.</param>
        /// <returns>The computed hash code.</returns>
        public static uint Murmur32(this ReadOnlySpan<byte> value, uint seed)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            using Murmur32 murmur = new(seed);
            murmur.TryComputeHash(value, buffer, out _);
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
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
        /// Computes the 128-bit hash value for the specified byte array using the murmur algorithm.
        /// </summary>
        /// <param name="value">The input to compute the hash code for.</param>
        /// <param name="seed">The seed used by the murmur algorithm.</param>
        /// <returns>The computed hash code.</returns>
        public static byte[] Murmur128(this ReadOnlySpan<byte> value, uint seed)
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(16);
            using Murmur128 murmur = new(seed);
            murmur.TryComputeHash(value, buffer, out _);
            return buffer;
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

        public static byte[] AES256Encrypt(this byte[] plainData, byte[] key, byte[] nonce, byte[] associatedData = null)
        {
            if (nonce.Length != 12) throw new ArgumentOutOfRangeException(nameof(nonce));
            var tag = new byte[16];
            var cipherBytes = new byte[plainData.Length];
            if (!IsOSX)
            {
                using var cipher = new AesGcm(key);
                cipher.Encrypt(nonce, plainData, cipherBytes, tag, associatedData);
            }
            else
            {
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(
                    new KeyParameter(key),
                    128, //128 = 16 * 8 => (tag size * 8)
                    nonce,
                    associatedData);
                cipher.Init(true, parameters);
                cipherBytes = new byte[cipher.GetOutputSize(plainData.Length)];
                var length = cipher.ProcessBytes(plainData, 0, plainData.Length, cipherBytes, 0);
                cipher.DoFinal(cipherBytes, length);
            }
            return Concat(nonce, cipherBytes, tag);
        }

        public static byte[] AES256Decrypt(this byte[] encryptedData, byte[] key, byte[] associatedData = null)
        {
            ReadOnlySpan<byte> encrypted = encryptedData;
            var nonce = encrypted[..12];
            var cipherBytes = encrypted[12..^16];
            var tag = encrypted[^16..];
            var decryptedData = new byte[cipherBytes.Length];
            if (!IsOSX)
            {
                using var cipher = new AesGcm(key);
                cipher.Decrypt(nonce, cipherBytes, tag, decryptedData, associatedData);
            }
            else
            {
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(
                    new KeyParameter(key),
                    128,  //128 = 16 * 8 => (tag size * 8)
                    nonce.ToArray(),
                    associatedData);
                cipher.Init(false, parameters);
                decryptedData = new byte[cipher.GetOutputSize(cipherBytes.Length)];
                var length = cipher.ProcessBytes(cipherBytes.ToArray(), 0, cipherBytes.Length, decryptedData, 0);
                cipher.DoFinal(decryptedData, length);
            }
            return decryptedData;
        }

        public static byte[] ECDHDeriveKey(KeyPair local, ECPoint remote)
        {
            ReadOnlySpan<byte> pubkey_local = local.PublicKey.EncodePoint(false);
            ReadOnlySpan<byte> pubkey_remote = remote.EncodePoint(false);
            using ECDiffieHellman ecdh1 = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = local.PrivateKey,
                Q = new System.Security.Cryptography.ECPoint
                {
                    X = pubkey_local[1..][..32].ToArray(),
                    Y = pubkey_local[1..][32..].ToArray()
                }
            });
            using ECDiffieHellman ecdh2 = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new System.Security.Cryptography.ECPoint
                {
                    X = pubkey_remote[1..][..32].ToArray(),
                    Y = pubkey_remote[1..][32..].ToArray()
                }
            });
            return ecdh1.DeriveKeyMaterial(ecdh2.PublicKey).Sha256();//z = r * P = r* k * G
        }

        internal static bool Test(this BloomFilter filter, Transaction tx)
        {
            if (filter.Check(tx.Hash.ToArray())) return true;
            if (tx.Signers.Any(p => filter.Check(p.Account.ToArray())))
                return true;
            return false;
        }
    }
}
