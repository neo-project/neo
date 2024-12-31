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
using System.Linq;
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
        /// 64 bytes ECDSA signature
        /// </summary>
        private const int SignatureLength = 64;

        private static readonly BigInteger s_prime = new(1,
            Hex.Decode("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F"));

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
        public static byte[] Sign(byte[] message, byte[] priKey, ECC.ECCurve ecCurve = null,
            Hasher hasher = Hasher.SHA256)
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

            using var ecdsa = ECDsa.Create(new ECParameters { Curve = curve, D = priKey, });
            var hashAlg =
                hasher == Hasher.SHA256 ? HashAlgorithmName.SHA256 : throw new NotSupportedException(nameof(hasher));
            return ecdsa.SignData(message, hashAlg);
        }


        /// <summary>
        /// Recovers the public key from a signature and message hash.
        /// </summary>
        /// <param name="signature">Signature, either 65 bytes (r[32] || s[32] || v[1]) or
        ///                         64 bytes in “compact” form (r[32] || yParityAndS[32]).</param>
        /// <param name="hash">32-byte message hash</param>
        /// <returns>The recovered public key</returns>
        /// <exception cref="ArgumentException">Thrown if signature or hash is invalid</exception>
        public static ECC.ECPoint ECRecover(byte[] signature, byte[] hash)
        {
            if (signature.Length != 65 && signature.Length != 64)
                throw new ArgumentException("Signature must be 65 or 64 bytes", nameof(signature));
            if (hash is not { Length: 32 })
                throw new ArgumentException("Message hash must be 32 bytes", nameof(hash));

            try
            {
                // Extract (r, s) and compute integer recId
                BigInteger r, s;
                int recId;

                if (signature.Length == 65)
                {
                    // Format: r[32] || s[32] || v[1]
                    r = new BigInteger(1, [.. signature.Take(32)]);
                    s = new BigInteger(1, [.. signature.Skip(32).Take(32)]);

                    // v could be 0..3 or 27..30 (Ethereum style).
                    var v = signature[64];
                    recId = v >= 27 ? v - 27 : v;  // normalize
                    if (recId < 0 || recId > 3)
                        throw new ArgumentException("Recovery value must be in [0..3] after normalization.", nameof(signature));
                }
                else
                {
                    // 64 bytes “compact” format: r[32] || yParityAndS[32]
                    // yParity is fused into the top bit of s.

                    r = new BigInteger(1, [.. signature.Take(32)]);
                    var yParityAndS = new BigInteger(1, signature.Skip(32).ToArray());

                    // Mask out top bit to get s
                    var mask = BigInteger.One.ShiftLeft(255).Subtract(BigInteger.One);
                    s = yParityAndS.And(mask);

                    // Extract yParity (0 or 1)
                    bool yParity = yParityAndS.TestBit(255);

                    // For “compact,” map parity to recId in [0..1].
                    // For typical usage, recId in {0,1} is enough:
                    recId = yParity ? 1 : 0;
                }

                // Decompose recId into i = recId >> 1 and yBit = recId & 1
                int iPart = recId >> 1;   // usually 0..1
                bool yBit = (recId & 1) == 1;

                // BouncyCastle curve constants
                var n = ECC.ECCurve.Secp256k1.BouncyCastleCurve.N;
                var e = new BigInteger(1, hash);

                // eInv = -e mod n
                var eInv = BigInteger.Zero.Subtract(e).Mod(n);
                // rInv = (r^-1) mod n
                var rInv = r.ModInverse(n);
                // srInv = (s * r^-1) mod n
                var srInv = rInv.Multiply(s).Mod(n);
                // eInvrInv = (eInv * r^-1) mod n
                var eInvrInv = rInv.Multiply(eInv).Mod(n);

                // x = r + iPart * n
                var x = r.Add(BigInteger.ValueOf(iPart).Multiply(n));
                // Verify x is within the curve prime
                if (x.CompareTo(s_prime) >= 0)
                    throw new ArgumentException("x is out of range of the secp256k1 prime.", nameof(signature));

                // Decompress to get R
                var decompressedRKey = DecompressKey(ECC.ECCurve.Secp256k1.BouncyCastleCurve.Curve, x, yBit);
                // Check that R is on curve
                if (!decompressedRKey.Multiply(n).IsInfinity)
                    throw new ArgumentException("R point is not valid on this curve.", nameof(signature));

                // Q = (eInv * G) + (srInv * R)
                var q = Org.BouncyCastle.Math.EC.ECAlgorithms.SumOfTwoMultiplies(
                    ECC.ECCurve.Secp256k1.BouncyCastleCurve.G, eInvrInv,
                    decompressedRKey, srInv);

                return ECC.ECPoint.FromBytes(q.Normalize().GetEncoded(false), ECC.ECCurve.Secp256k1);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid signature parameters", nameof(signature), ex);
            }
        }

        private static Org.BouncyCastle.Math.EC.ECPoint DecompressKey(
            Org.BouncyCastle.Math.EC.ECCurve curve, BigInteger xBN, bool yBit)
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
        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ECC.ECPoint pubkey,
            Hasher hasher = Hasher.SHA256)
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
                hasher == Hasher.SHA256 ? HashAlgorithmName.SHA256 : throw new NotSupportedException(nameof(hasher));
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
                Q = new ECPoint { X = buffer[1..33], Y = buffer[33..] }
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
        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature,
            ReadOnlySpan<byte> pubkey, ECC.ECCurve curve, Hasher hasher = Hasher.SHA256)
        {
            return VerifySignature(message, signature, ECC.ECPoint.DecodePoint(pubkey, curve), hasher);
        }
    }
}
