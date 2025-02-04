// Copyright (C) 2015-2025 The Neo Project.
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
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// A native contract library that provides cryptographic algorithms.
    /// </summary>
    public sealed partial class CryptoLib : NativeContract
    {
        private static readonly Dictionary<NamedCurveHash, (ECCurve Curve, HashAlgorithm HashAlgorithm)> s_curves = new()
        {
            [NamedCurveHash.secp256k1SHA256] = (ECCurve.Secp256k1, HashAlgorithm.SHA256),
            [NamedCurveHash.secp256r1SHA256] = (ECCurve.Secp256r1, HashAlgorithm.SHA256),
            [NamedCurveHash.secp256k1Keccak256] = (ECCurve.Secp256k1, HashAlgorithm.Keccak256),
            [NamedCurveHash.secp256r1Keccak256] = (ECCurve.Secp256r1, HashAlgorithm.Keccak256),
        };

        internal CryptoLib() : base() { }

        /// <summary>
        /// Recovers the public key from a secp256k1 signature in a single byte array format.
        /// </summary>
        /// <param name="message">The original message that was signed.</param>
        /// <param name="signature">The 65-byte signature in format: r[32] + s[32] + v[1]. 64-bytes for eip-2098, where v must be 27 or 28.</param>
        /// <param name="hashAlgorithm">The hash algorithm to be used hash the message.</param>
        /// <returns>The recovered public key in compressed format, or null if recovery fails.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 10, Name = "recoverSecp256K1")]
        public static byte[] RecoverSecp256K1(byte[] message, byte[] signature, HashAlgorithm hashAlgorithm)
        {
            // It will be checked in Crypto.ECRecover
            // if (signature.Length != 65 && signature.Length != 64)
            //    throw new ArgumentException("Signature must be 65 or 64 bytes", nameof(signature));

            try
            {
                var messageHash = Crypto.GetMessageHash(message, hashAlgorithm);
                if (messageHash == null) return null;

                var point = Crypto.ECRecover(signature, messageHash);
                return point?.EncodePoint(true);
            }
            catch
            {
                return null;
            }
        }

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
            Murmur32 murmur = new(seed);
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
                return Crypto.VerifySignature(message, signature, pubkey, ch.Curve, ch.HashAlgorithm);
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
        /// Verifies that a digital signature is appropriate for the provided key and message using the Ed25519 algorithm.
        /// </summary>
        /// <param name="message">The signed message.</param>
        /// <param name="publicKey">The Ed25519 public key to be used.</param>
        /// <param name="signature">The signature to be verified.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        [ContractMethod(Hardfork.HF_Echidna, CpuFee = 1 << 15)]
        public static bool VerifyWithEd25519(byte[] message, byte[] publicKey, byte[] signature)
        {
            if (signature.Length != Ed25519.SignatureSize)
                return false;

            if (publicKey.Length != Ed25519.PublicKeySize)
                return false;

            try
            {
                var verifier = new Ed25519Signer();
                verifier.Init(false, new Ed25519PublicKeyParameters(publicKey, 0));
                verifier.BlockUpdate(message, 0, message.Length);
                return verifier.VerifySignature(signature);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
