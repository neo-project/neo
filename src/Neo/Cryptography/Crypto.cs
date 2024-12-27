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
using Neo.SmartContract.Native;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities.Encoders;
using System;
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
        /// 64 bytes ECDSA signature + 1 byte recovery id
        /// </summary>
        private const int RecoverableSignatureLength = 64 + 1;
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
                var messageHash = CryptoLib.GetMessageHash(message, hasher);
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
        /// Recovers the public key from a signature and message hash.
        /// </summary>
        /// <param name="signature">65-byte signature (r[32] || s[32] || v[1])</param>
        /// <param name="hash">32-byte message hash</param>
        /// <returns>The recovered public key</returns>
        /// <exception cref="ArgumentException">Thrown when signature or hash parameters are invalid</exception>
        public static ECC.ECPoint ECRecover(byte[] signature, byte[] hash)
        {
            if (signature.Length != RecoverableSignatureLength)
                throw new ArgumentException("Signature must be 65 bytes with recovery value", nameof(signature));
            if (hash.Length != 32)
                throw new ArgumentException("Message hash must be 32 bytes", nameof(hash));

            try
            {
                // Extract r, s components (32 bytes each)
                var r = new BigInteger(1, signature.Take(32).ToArray());
                var s = new BigInteger(1, signature.Skip(32).Take(32).ToArray());

                // Get recovery id, allowing both 0-3 and 27-30
                var v = signature[SignatureLength];
                var recId = v >= 27 ? v - 27 : v;
                if (recId > 3)
                    throw new ArgumentException("Recovery value must be 0-3 or 27-30", nameof(signature));

                // Get curve parameters
                var curve = ECC.ECCurve.Secp256k1.BouncyCastleCurve;
                var n = curve.N;

                // Validate r, s values
                if (r.SignValue <= 0 || r.CompareTo(n) >= 0)
                    throw new ArgumentException("r must be in range [1, N-1]", nameof(signature));
                if (s.SignValue <= 0 || s.CompareTo(n) >= 0)
                    throw new ArgumentException("s must be in range [1, N-1]", nameof(signature));

                // Calculate x coordinate
                var i = BigInteger.ValueOf(recId >> 1);
                var x = r.Add(i.Multiply(n));

                // Get curve field
                var field = curve.Curve.Field;
                if (x.CompareTo(field.Characteristic) >= 0)
                    throw new ArgumentException("Invalid x coordinate", nameof(signature));

                // Convert x to field element
                var xField = curve.Curve.FromBigInteger(x);

                // Compute right-hand side of curve equation: y^2 = x^3 + ax + b
                var rhs = xField.Square().Multiply(xField).Add(curve.Curve.A.Multiply(xField)).Add(curve.Curve.B);

                // Compute y coordinate
                var y = rhs.Sqrt();
                if (y == null)
                    throw new ArgumentException("Invalid x coordinate - no square root exists", nameof(signature));

                // Ensure y has correct parity
                if (y.ToBigInteger().TestBit(0) != ((recId & 1) == 1))
                    y = y.Negate();

                // Create R point
                var R = curve.Curve.CreatePoint(x, y.ToBigInteger());

                // Check R * n = infinity
                if (!R.Multiply(n).IsInfinity)
                    throw new ArgumentException("Invalid R point order", nameof(signature));

                // Calculate e = -hash mod n
                var e = new BigInteger(1, hash).Negate().Mod(n);

                // Calculate r^-1
                var rInv = r.ModInverse(n);

                // Calculate Q = r^-1 (sR - eG)
                var Q = R.Multiply(s).Add(curve.G.Multiply(e)).Multiply(rInv);

                if (Q.IsInfinity)
                    throw new ArgumentException("Invalid public key point at infinity", nameof(signature));

                // Convert to Neo ECPoint format
                return ECC.ECPoint.FromBytes(Q.GetEncoded(false), ECC.ECCurve.Secp256k1);
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
                var messageHash = CryptoLib.GetMessageHash(message, hasher);

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
