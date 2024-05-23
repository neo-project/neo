// Copyright (C) 2015-2024 The Neo Project.
//
// Crypto.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Caching;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    /// <summary>
    /// A cryptographic helper class.
    /// </summary>
    public static class Crypto
    {
        private static readonly ECDsaCache CacheECDsa = new();
        private static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private static readonly ECCurve secP256k1 = ECCurve.CreateFromFriendlyName("secP256k1");
        private static readonly X9ECParameters bouncySecp256k1 = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
        private static readonly X9ECParameters bouncySecp256r1 = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256r1");

        /// <summary>
        /// Holds domain parameters for Secp256r1 elliptic curve.
        /// </summary>
        private static readonly ECDomainParameters secp256r1DomainParams = new ECDomainParameters(bouncySecp256r1.Curve, bouncySecp256r1.G, bouncySecp256r1.N, bouncySecp256r1.H);

        /// <summary>
        /// Holds domain parameters for Secp256k1 elliptic curve.
        /// </summary>
        private static readonly ECDomainParameters secp256k1DomainParams = new ECDomainParameters(bouncySecp256k1.Curve, bouncySecp256k1.G, bouncySecp256k1.N, bouncySecp256k1.H);

        /// <summary>
        /// Calculates the 160-bit hash value of the specified message.
        /// </summary>
        /// <param name="message">The message to be hashed.</param>
        /// <returns>160-bit hash value.</returns>
        public static byte[] Hash160(ReadOnlySpan<byte> message)
        {
            return message.Sha256().RIPEMD160();
        }

        /// <summary>
        /// Calculates the 256-bit hash value of the specified message.
        /// </summary>
        /// <param name="message">The message to be hashed.</param>
        /// <returns>256-bit hash value.</returns>
        public static byte[] Hash256(ReadOnlySpan<byte> message)
        {
            return message.Sha256().Sha256();
        }

        /// <summary>
        /// Signs the specified message using the ECDSA algorithm and specified hash algorithm.
        /// </summary>
        /// <param name="message">The message to be signed.</param>
        /// <param name="priKey">The private key to be used.</param>
        /// <param name="ecCurve">The <see cref="ECC.ECCurve"/> curve of the signature, default is <see cref="ECC.ECCurve.Secp256r1"/>.</param>
        /// <param name="hasher">The hash algorithm to hash the message, default is SHA256.</param>
        /// <returns>The ECDSA signature for the specified message.</returns>
        public static byte[] Sign(byte[] message, byte[] priKey, ECC.ECCurve ecCurve = null, Hasher hasher = Hasher.SHA256)
        {
            if (hasher == Hasher.Keccak256 || (IsOSX && ecCurve == ECC.ECCurve.Secp256k1))
            {
                var domain =
                    ecCurve == null || ecCurve == ECC.ECCurve.Secp256r1 ? secp256r1DomainParams :
                    ecCurve == ECC.ECCurve.Secp256k1 ? secp256k1DomainParams :
                    throw new NotSupportedException(nameof(ecCurve));
                var signer = new Org.BouncyCastle.Crypto.Signers.ECDsaSigner();
                var privateKey = new BigInteger(1, priKey);
                var priKeyParameters = new ECPrivateKeyParameters(privateKey, domain);
                signer.Init(true, priKeyParameters);
                var messageHash =
                    hasher == Hasher.SHA256 ? message.Sha256() :
                    hasher == Hasher.Keccak256 ? message.Keccak256() :
                    throw new NotSupportedException(nameof(hasher));
                var signature = signer.GenerateSignature(messageHash);

                var signatureBytes = new byte[64];
                var rBytes = signature[0].ToByteArrayUnsigned();
                var sBytes = signature[1].ToByteArrayUnsigned();

                // Copy r and s into their respective parts of the signatureBytes array, aligning them to the right.
                Buffer.BlockCopy(rBytes, 0, signatureBytes, 32 - rBytes.Length, rBytes.Length);
                Buffer.BlockCopy(sBytes, 0, signatureBytes, 64 - sBytes.Length, sBytes.Length);
                return signatureBytes;
            }

            var curve =
                ecCurve == null || ecCurve == ECC.ECCurve.Secp256r1 ? ECCurve.NamedCurves.nistP256 :
                ecCurve == ECC.ECCurve.Secp256k1 ? secP256k1 :
                throw new NotSupportedException();

            using var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = curve,
                D = priKey,
            });
            var hashAlg =
                hasher == Hasher.SHA256 ? HashAlgorithmName.SHA256 :
                throw new NotSupportedException(nameof(hasher));
            return ecdsa.SignData(message, hashAlg);
        }

        /// <summary>
        /// Verifies that a digital signature is appropriate for the provided key, message and hash algorithm.
        /// </summary>
        /// <param name="message">The signed message.</param>
        /// <param name="signature">The signature to be verified.</param>
        /// <param name="pubkey">The public key to be used.</param>
        /// <param name="hasher">The hash algorithm to be used to hash the message, the default is SHA256.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ECC.ECPoint pubkey, Hasher hasher = Hasher.SHA256)
        {
            if (signature.Length != 64) return false;

            if (hasher == Hasher.Keccak256 || (IsOSX && pubkey.Curve == ECC.ECCurve.Secp256k1))
            {
                var domain =
                    pubkey.Curve == ECC.ECCurve.Secp256r1 ? secp256r1DomainParams :
                    pubkey.Curve == ECC.ECCurve.Secp256k1 ? secp256k1DomainParams :
                    throw new NotSupportedException(nameof(pubkey.Curve));
                var curve =
                    pubkey.Curve == ECC.ECCurve.Secp256r1 ? bouncySecp256r1.Curve :
                    bouncySecp256k1.Curve;

                var point = curve.CreatePoint(
                    new BigInteger(pubkey.X.Value.ToString()),
                    new BigInteger(pubkey.Y.Value.ToString()));
                var pubKey = new ECPublicKeyParameters("ECDSA", point, domain);
                var signer = new Org.BouncyCastle.Crypto.Signers.ECDsaSigner();
                signer.Init(false, pubKey);

                var sig = signature.ToArray();
                var r = new BigInteger(1, sig, 0, 32);
                var s = new BigInteger(1, sig, 32, 32);

                var messageHash =
                    hasher == Hasher.SHA256 ? message.Sha256() :
                    hasher == Hasher.Keccak256 ? message.Keccak256() :
                    throw new NotSupportedException(nameof(hasher));

                return signer.VerifySignature(messageHash, r, s);
            }

            var ecdsa = CreateECDsa(pubkey);
            var hashAlg =
                hasher == Hasher.SHA256 ? HashAlgorithmName.SHA256 :
                throw new NotSupportedException(nameof(hasher));
            return ecdsa.VerifyData(message, signature, hashAlg);
        }

        /// <summary>
        /// Create and cache ECDsa objects
        /// </summary>
        /// <param name="pubkey"></param>
        /// <returns>Cached ECDsa</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static ECDsa CreateECDsa(ECC.ECPoint pubkey)
        {
            if (CacheECDsa.TryGet(pubkey, out var cache))
            {
                return cache.value;
            }
            var curve =
                pubkey.Curve == ECC.ECCurve.Secp256r1 ? ECCurve.NamedCurves.nistP256 :
                pubkey.Curve == ECC.ECCurve.Secp256k1 ? secP256k1 :
                throw new NotSupportedException();
            var buffer = pubkey.EncodePoint(false);
            var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = curve,
                Q = new ECPoint
                {
                    X = buffer[1..33],
                    Y = buffer[33..]
                }
            });
            CacheECDsa.Add(new ECDsaCacheItem(pubkey, ecdsa));
            return ecdsa;
        }

        /// <summary>
        /// Verifies that a digital signature is appropriate for the provided key, curve, message and hasher.
        /// </summary>
        /// <param name="message">The signed message.</param>
        /// <param name="signature">The signature to be verified.</param>
        /// <param name="pubkey">The public key to be used.</param>
        /// <param name="curve">The curve to be used by the ECDSA algorithm.</param>
        /// <param name="hasher">The hash algorithm to be used hash the message, the default is SHA256.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> pubkey, ECC.ECCurve curve, Hasher hasher = Hasher.SHA256)
        {
            return VerifySignature(message, signature, ECC.ECPoint.DecodePoint(pubkey, curve), hasher);
        }
    }
}
