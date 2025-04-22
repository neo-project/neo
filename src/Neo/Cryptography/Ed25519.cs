// Copyright (C) 2015-2025 The Neo Project.
//
// Ed25519.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using System;

namespace Neo.Cryptography
{
    public class Ed25519
    {
        internal const int PublicKeySize = 32;
        private const int PrivateKeySize = 32;
        internal const int SignatureSize = 64;

        /// <summary>
        /// Generates a new Ed25519 key pair.
        /// </summary>
        /// <returns>A byte array containing the private key.</returns>
        public static byte[] GenerateKeyPair()
        {
            var keyPairGenerator = new Ed25519KeyPairGenerator();
            keyPairGenerator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
            var keyPair = keyPairGenerator.GenerateKeyPair();
            return ((Ed25519PrivateKeyParameters)keyPair.Private).GetEncoded();
        }

        /// <summary>
        /// Derives the public key from a given private key.
        /// </summary>
        /// <param name="privateKey">The private key as a byte array.</param>
        /// <returns>The corresponding public key as a byte array.</returns>
        /// <exception cref="ArgumentException">Thrown when the private key size is invalid.</exception>
        public static byte[] GetPublicKey(byte[] privateKey)
        {
            if (privateKey.Length != PrivateKeySize)
                throw new ArgumentException("Invalid private key size", nameof(privateKey));

            var privateKeyParams = new Ed25519PrivateKeyParameters(privateKey, 0);
            return privateKeyParams.GeneratePublicKey().GetEncoded();
        }

        /// <summary>
        /// Signs a message using the provided private key.
        /// Parameters are in the same order as the sample in the Ed25519 specification
        /// Ed25519.sign(privkey, pubkey, msg) with pubkey omitted
        /// ref. https://datatracker.ietf.org/doc/html/rfc8032.
        /// </summary>
        /// <param name="privateKey">The private key used for signing.</param>
        /// <param name="message">The message to be signed.</param>
        /// <returns>The signature as a byte array.</returns>
        /// <exception cref="ArgumentException">Thrown when the private key size is invalid.</exception>
        public static byte[] Sign(byte[] privateKey, byte[] message)
        {
            if (privateKey.Length != PrivateKeySize)
                throw new ArgumentException("Invalid private key size", nameof(privateKey));

            var signer = new Ed25519Signer();
            signer.Init(true, new Ed25519PrivateKeyParameters(privateKey, 0));
            signer.BlockUpdate(message, 0, message.Length);
            return signer.GenerateSignature();
        }

        /// <summary>
        /// Verifies an Ed25519 signature for a given message using the provided public key.
        /// Parameters are in the same order as the sample in the Ed25519 specification
        /// Ed25519.verify(public, msg, signature)
        /// ref. https://datatracker.ietf.org/doc/html/rfc8032.
        /// </summary>
        /// <param name="publicKey">The 32-byte public key used for verification.</param>
        /// <param name="message">The message that was signed.</param>
        /// <param name="signature">The 64-byte signature to verify.</param>
        /// <returns>True if the signature is valid for the given message and public key; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when the signature or public key size is invalid.</exception>
        public static bool Verify(byte[] publicKey, byte[] message, byte[] signature)
        {
            if (signature.Length != SignatureSize)
                throw new ArgumentException("Invalid signature size", nameof(signature));

            if (publicKey.Length != PublicKeySize)
                throw new ArgumentException("Invalid public key size", nameof(publicKey));

            var verifier = new Ed25519Signer();
            verifier.Init(false, new Ed25519PublicKeyParameters(publicKey, 0));
            verifier.BlockUpdate(message, 0, message.Length);
            return verifier.VerifySignature(signature);
        }
    }
}
