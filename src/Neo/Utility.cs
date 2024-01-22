// Copyright (C) 2015-2024 The Neo Project.
//
// Utility.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Event;
using Neo.Cryptography.BloomFilter;
using Neo.Cryptography.Utility;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.Wallets;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Neo
{
    public delegate void LogEventHandler(string source, LogLevel level, object message);

    /// <summary>
    /// A utility class that provides common functions.
    /// </summary>
    public static class Utility
    {
        internal class Logger : ReceiveActor
        {
            public Logger()
            {
                Receive<InitializeLogger>(_ => Sender.Tell(new LoggerInitialized()));
                Receive<LogEvent>(e => Log(e.LogSource, (LogLevel)e.LogLevel(), e.Message));
            }
        }

        public static event LogEventHandler Logging;

        /// <summary>
        /// A strict UTF8 encoding used in NEO system.
        /// </summary>
        public static Encoding StrictUTF8 { get; }

        static Utility()
        {
            StrictUTF8 = (Encoding)Encoding.UTF8.Clone();
            StrictUTF8.DecoderFallback = DecoderFallback.ExceptionFallback;
            StrictUTF8.EncoderFallback = EncoderFallback.ExceptionFallback;
        }

        /// <summary>
        /// Writes a log.
        /// </summary>
        /// <param name="source">The source of the log. Used to identify the producer of the log.</param>
        /// <param name="level">The level of the log.</param>
        /// <param name="message">The message of the log.</param>
        public static void Log(string source, LogLevel level, object message)
        {
            Logging?.Invoke(source, level, message);
        }

        public static byte[] ECDHDeriveKey(KeyPair local, ECC.ECPoint remote)
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


        internal static byte[] ECDHDeriveKey(ECC.ECPoint publicKey, ReadOnlySpan<byte> privateKey, ECC.ECPoint remote)
        {
            ReadOnlySpan<byte> pubkey_local = publicKey.EncodePoint(false);
            ReadOnlySpan<byte> pubkey_remote = remote.EncodePoint(false);
            using ECDiffieHellman ecdh1 = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = privateKey.ToArray(),
                Q = new ECPoint
                {
                    X = pubkey_local[1..][..32].ToArray(),
                    Y = pubkey_local[1..][32..].ToArray()
                }
            });
            using ECDiffieHellman ecdh2 = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
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

        private static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

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
            return Neo.Helper.Concat(nonce, cipherBytes, tag);
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

        /// <summary>
        /// Signs the specified message using the ECDSA algorithm.
        /// </summary>
        /// <param name="message">The message to be signed.</param>
        /// <param name="prikey">The private key to be used.</param>
        /// <param name="pubkey">The public key to be used.</param>
        /// <returns>The ECDSA signature for the specified message.</returns>
        public static byte[] Sign(byte[] message, byte[] prikey, byte[] pubkey)
        {
            using var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = prikey,
                Q = new ECPoint
                {
                    X = pubkey[..32],
                    Y = pubkey[32..]
                }
            });
            return ecdsa.SignData(message, HashAlgorithmName.SHA256);
        }

        /// <summary>
        /// Verifies that a digital signature is appropriate for the provided key and message.
        /// </summary>
        /// <param name="message">The signed message.</param>
        /// <param name="signature">The signature to be verified.</param>
        /// <param name="pubkey">The public key to be used.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ECC.ECPoint pubkey)
        {
            if (signature.Length != 64) return false;

            if (IsOSX && pubkey.Curve == Neo.ECC.ECCurve.Secp256k1)
            {
                var curve = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
                var domain = new Org.BouncyCastle.Crypto.Parameters.ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
                var point = curve.Curve.CreatePoint(
                    new Org.BouncyCastle.Math.BigInteger(pubkey.X.Value.ToString()),
                    new Org.BouncyCastle.Math.BigInteger(pubkey.Y.Value.ToString()));
                var pubKey = new Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters("ECDSA", point, domain);
                var signer = new Org.BouncyCastle.Crypto.Signers.ECDsaSigner();
                signer.Init(false, pubKey);

                var sig = signature.ToArray();
                var r = new Org.BouncyCastle.Math.BigInteger(1, sig, 0, 32);
                var s = new Org.BouncyCastle.Math.BigInteger(1, sig, 32, 32);

                return signer.VerifySignature(message.Sha256(), r, s);
            }
            else
            {
                ECCurve curve =
                    pubkey.Curve == Neo.ECC.ECCurve.Secp256r1 ? ECCurve.NamedCurves.nistP256 :
                    pubkey.Curve == Neo.ECC.ECCurve.Secp256k1 ? ECCurve.CreateFromFriendlyName("secP256k1") :
                    throw new NotSupportedException();
                byte[] buffer = pubkey.EncodePoint(false);
                using var ecdsa = ECDsa.Create(new ECParameters
                {
                    Curve = curve,
                    Q = new ECPoint
                    {
                        X = buffer[1..33],
                        Y = buffer[33..]
                    }
                });
                return ecdsa.VerifyData(message, signature, HashAlgorithmName.SHA256);
            }
        }

        /// <summary>
        /// Verifies that a digital signature is appropriate for the provided key and message.
        /// </summary>
        /// <param name="message">The signed message.</param>
        /// <param name="signature">The signature to be verified.</param>
        /// <param name="pubkey">The public key to be used.</param>
        /// <param name="curve">The curve to be used by the ECDSA algorithm.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        public static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> pubkey, Neo.ECC.ECCurve curve)
        {
            return VerifySignature(message, signature, ECC.ECPoint.DecodePoint(pubkey, curve));
        }
    }
}
