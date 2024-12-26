// Copyright (C) 2015-2024 The Neo Project.
//
// CryptoLib.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract library that provides cryptographic algorithms.
    /// </summary>
    public sealed partial class CryptoLib : NativeContract
    {
        private static readonly Dictionary<NamedCurveHash, (ECCurve Curve, Hasher Hasher)> s_curves = new()
        {
            [NamedCurveHash.secp256k1SHA256] = (ECCurve.Secp256k1, Hasher.SHA256),
            [NamedCurveHash.secp256r1SHA256] = (ECCurve.Secp256r1, Hasher.SHA256),
            [NamedCurveHash.secp256k1Keccak256] = (ECCurve.Secp256k1, Hasher.Keccak256),
            [NamedCurveHash.secp256r1Keccak256] = (ECCurve.Secp256r1, Hasher.Keccak256),
        };

        internal CryptoLib() : base() { }

        /// <summary>
        /// Computes the hash value for the specified byte array using the ripemd160 algorithm.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        [ContractMethod(CpuFee = 1 << 15, Name = "ripemd160")]
        public static byte[] RIPEMD160(byte[] data)
        {
            return data.RIPEMD160();
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the sha256 algorithm.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <returns>The computed hash code.</returns>
        [ContractMethod(CpuFee = 1 << 15)]
        public static byte[] Sha256(byte[] data)
        {
            return data.Sha256();
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the murmur32 algorithm.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <param name="seed">The seed of the murmur32 hash function</param>
        /// <returns>The computed hash code.</returns>
        [ContractMethod(CpuFee = 1 << 13)]
        public static byte[] Murmur32(byte[] data, uint seed)
        {
            using Murmur32 murmur = new(seed);
            return murmur.ComputeHash(data);
        }

        /// <summary>
        /// Computes the hash value for the specified byte array using the keccak256 algorithm.
        /// </summary>
        /// <param name="data">The input to compute the hash code for.</param>
        /// <returns>Computed hash</returns>
        [ContractMethod(Hardfork.HF_Cockatrice, CpuFee = 1 << 15)]
        public static byte[] Keccak256(byte[] data)
        {
            return data.Keccak256();
        }

        private static byte[] GetMessageHash(byte[] message, Hasher hasher)
        {
            return hasher switch
            {
                Hasher.SHA256 => message.Sha256(),
                Hasher.Keccak256 => message.Keccak256(),
                _ => null
            };
        }

        private static ECPoint ECrecover(byte[] message, byte[] signature, Hasher hasher)
        {
            var messageHash = GetMessageHash(message, hasher);
            if (messageHash == null) return null;

            try
            {
                return Crypto.ECRecover(signature, messageHash);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Verifies that a digital signature is appropriate for the provided key and message using the ECDSA algorithm.
        /// </summary>
        /// <param name="message">The signed message.</param>
        /// <param name="pubkey">The public key to be used.</param>
        /// <param name="signature">The signature to be verified.</param>
        /// <param name="curveHash">A pair of the curve to be used by the ECDSA algorithm and the hasher function to be used to hash message.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        [ContractMethod(Hardfork.HF_Cockatrice, CpuFee = 1 << 15)]
        public static bool VerifyWithECDsa(byte[] message, byte[] pubkey, byte[] signature, NamedCurveHash curveHash)
        {
            try
            {
                var ch = s_curves[curveHash];
                return Crypto.VerifySignature(message, signature, pubkey, ch.Curve, ch.Hasher);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        // This is for solving the hardfork issue in https://github.com/neo-project/neo/pull/3209
        [ContractMethod(true, Hardfork.HF_Cockatrice, CpuFee = 1 << 15, Name = "verifyWithECDsa")]
        public static bool VerifyWithECDsaV0(byte[] message, byte[] pubkey, byte[] signature, NamedCurveHash curve)
        {
            if (curve != NamedCurveHash.secp256k1SHA256 && curve != NamedCurveHash.secp256r1SHA256)
                throw new ArgumentOutOfRangeException(nameof(curve));

            try
            {
                return Crypto.VerifySignature(message, signature, pubkey, s_curves[curve].Curve);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Recovers the public key from a secp256k1 signature in a single byte array format.
        /// </summary>
        /// <param name="message">The original message that was signed.</param>
        /// <param name="hasher">The hash algorithm to be used (SHA256 or Keccak256).</param>
        /// <param name="signature">The 65-byte signature in format: r[32] + s[32] + v[1], where v must be 27 or 28.</param>
        /// <returns>The recovered public key in compressed format, or null if recovery fails.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 10, Name = "secp256k1Recover")]
        public static byte[] Secp256K1Recover(byte[] message, Hasher hasher, byte[] signature)
        {
            if (signature is not { Length: 65 })
                return null;

            var v = signature[64];
            if (v != 27 && v != 28)
                return null;

            var point = ECrecover(message, signature, hasher);

            return point?.EncodePoint(true);
        }

        /// <summary>
        /// Recovers the public key from a secp256k1 signature with separate r, s, and v components.
        /// </summary>
        /// <param name="message">The original message that was signed.</param>
        /// <param name="hasher">The hash algorithm to be used (SHA256 or Keccak256).</param>
        /// <param name="r">The r component of the signature (32 bytes).</param>
        /// <param name="s">The s component of the signature (32 bytes).</param>
        /// <param name="v">The recovery identifier (must be 27 or 28).</param>
        /// <returns>The recovered public key in compressed format, or null if recovery fails.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 10, Name = "secp256k1Recover")]
        public static byte[] Secp256K1Recover(byte[] message, Hasher hasher, byte[] r, byte[] s, BigInteger v)
        {
            if (r == null || s == null || r.Length != 32 || s.Length != 32)// || (v != 27 && v != 28)) Should we assume v as either 27/28
                return null;

            var signature = new byte[65];
            r.CopyTo(signature, 0);
            s.CopyTo(signature, 32);
            signature[64] = (byte)v;

            return Secp256K1Recover(message, hasher, signature);
        }
    }
}
