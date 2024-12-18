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
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    /// <summary>
    /// A cryptographic helper class.
    /// </summary>
    public static class Crypto
    {
        /// <summary>
        /// 64 bytes ECDSA signature + 1 byte recovery id
        /// </summary>
        private const int RecuperableSignatureLength = 64 + 1;
        /// <summary>
        /// 64 bytes ECDSA signature
        /// </summary>
        private const int SignatureLength = 64;
        private static readonly BigInteger s_prime = new(1, Hex.Decode("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F"));
        private static readonly ECDsaCache CacheECDsa = new();
        private static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private static readonly ECCurve secP256k1 = ECCurve.CreateFromFriendlyName("secP256k1");

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
                var signer = new Org.BouncyCastle.Crypto.Signers.ECDsaSigner();
                var privateKey = new BigInteger(1, priKey);
                var priKeyParameters = new ECPrivateKeyParameters(privateKey, ecCurve.BouncyCastleDomainParams);
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
        /// ECRecover
        /// </summary>
        /// <param name="curve">Curve</param>
        /// <param name="signature">Signature</param>
        /// <param name="hash">Message hash</param>
        /// <param name="format">Signature format</param>
        /// <returns>Allowed Public keys</returns>
        public static ECC.ECPoint[] ECRecover(ECC.ECCurve curve, byte[] signature, byte[] hash, SignatureFormat format = SignatureFormat.Der)
        {
            BigInteger r, s;
            int recId = 0, recIdTo = 4;

            // Decode signature

            switch (format)
            {
                case SignatureFormat.Der:
                    {
                        var derSequence = (DerSequence)Asn1Object.FromByteArray(signature);
                        r = ((DerInteger)derSequence[0]).Value;
                        s = ((DerInteger)derSequence[1]).Value;

                        if (derSequence.Count == 3)
                        {
                            recId = ((DerInteger)derSequence[2]).IntValueExact;
                            recIdTo = recId + 1;
                        }
                        break;
                    }
                case SignatureFormat.Fixed32:
                    {
                        r = new(1, signature, 0, 32);
                        s = new(1, signature, 32, 32);

                        if (signature.Length == RecuperableSignatureLength)
                        {
                            recId = signature[SignatureLength];
                            recIdTo = recId + 1;
                        }
                        break;
                    }
                default: throw new InvalidOperationException("Invalid signature format");
            }

            // Validate values

            if (recId < 0 || recId >= 4) throw new ArgumentException("v should be positive less than 4");
            if (r.SignValue < 0) throw new ArgumentException("r should be positive");
            if (s.SignValue < 0) throw new ArgumentException("s should be positive");

            // Precompute variables

            var n = curve.BouncyCastleCurve.N;
            var e = new BigInteger(1, hash);
            var eInv = BigInteger.Zero.Subtract(e).Mod(n);
            var rInv = r.ModInverse(n);
            var srInv = rInv.Multiply(s).Mod(n);
            var eInvrInv = rInv.Multiply(eInv).Mod(n);

            // Do the work

            var recovered = new List<ECC.ECPoint>();

            for (; recId < recIdTo; ++recId)
            {
                var i = BigInteger.ValueOf((long)recId / 2);
                var x = r.Add(i.Multiply(n));

                if (x.CompareTo(s_prime) >= 0)
                {
                    continue;
                }

                var decompressedRKey = DecompressKey(curve.BouncyCastleCurve.Curve, x, (recId & 1) == 1);
                if (!decompressedRKey.Multiply(n).IsInfinity)
                {
                    continue;
                }

                var q = Org.BouncyCastle.Math.EC.ECAlgorithms.SumOfTwoMultiplies(curve.BouncyCastleCurve.G, eInvrInv, decompressedRKey, srInv);
                recovered.Add(ECC.ECPoint.FromBytes(q.Normalize().GetEncoded(false), curve));
            }

            return [.. recovered];
        }

        /// <summary>
        /// Decompress key
        /// </summary>
        /// <param name="curve">ECC curve</param>
        /// <param name="xBN">xBN</param>
        /// <param name="yBit">yBit</param>
        /// <returns>ECPoint</returns>
        private static Org.BouncyCastle.Math.EC.ECPoint DecompressKey(Org.BouncyCastle.Math.EC.ECCurve curve, BigInteger xBN, bool yBit)
        {
            var compEnc = X9IntegerConverter.IntegerToBytes(xBN, 1 + X9IntegerConverter.GetByteLength(curve));
            compEnc[0] = (byte)(yBit ? 0x03 : 0x02);
            return curve.DecodePoint(compEnc);
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
                var point = pubkey.Curve.BouncyCastleCurve.Curve.CreatePoint(
                    new BigInteger(pubkey.X.Value.ToString()),
                    new BigInteger(pubkey.Y.Value.ToString()));
                var pubKey = new ECPublicKeyParameters("ECDSA", point, pubkey.Curve.BouncyCastleDomainParams);
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
                return cache.Value;
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
