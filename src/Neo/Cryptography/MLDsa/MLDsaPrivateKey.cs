// Copyright (C) 2015-2025 The Neo Project.
//
// MLDsaPrivateKey.cs file belongs to the neo project and is free
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
    public readonly struct MLDsaPrivateKey
    {
        private readonly MLDsaPrivateKeyParameters _privateKey;

        public MLDsaPrivateKey(MLDsaPrivateKeyParameters privateKey)
        {
            _privateKey = privateKey;
        }

        public static MLDsaPrivateKey CreateMLDsa65()
        {
            // C# standard library does not all platforms, so we use BouncyCastle.
            var generator = new MLDsaKeyPairGenerator();
            generator.Init(new MLDsaKeyGenerationParameters(new SecureRandom(), MLDsaParameters.ml_dsa_65));

            var keypair = generator.GenerateKeyPair();
            return new MLDsaPrivateKey((MLDsaPrivateKeyParameters)keypair.Private);
        }

        public MLDsaPublicKey PublicKey => new MLDsaPublicKey(_privateKey.GetPublicKey());

        /// <summary>
        /// Exports the private key of the MLDsa65 with the FIPS 204 key format.
        /// NOTE: use it carefully, because the private key is sensitive data.
        /// </summary>
        /// <returns>The private key in the FIPS 204 key format.</returns>
        public readonly byte[] ExportPrivateKey() => _privateKey.GetEncoded();

        /// <summary>
        /// Imports the private key of the MLDsa65 with the FIPS 204 key format.
        /// </summary>
        /// <param name="privateKey">The private key in the FIPS 204 key format.</param>
        /// <returns>The MLDsa65 private key imported from the private key.</returns>
        /// <exception cref="ArgumentException">The private key is invalid.</exception>
        public static MLDsaPrivateKey ImportMLDsa65Key(byte[] privateKey)
        {
            var key = MLDsaPrivateKeyParameters.FromEncoding(MLDsaParameters.ml_dsa_65, privateKey);
            return new MLDsaPrivateKey(key);
        }

        /// <summary>
        /// Signs the specified message with the MLDsa65.
        /// </summary>
        /// <param name="message">The message to sign.</param>
        /// <returns>The signature of the message.</returns>
        public readonly byte[] Sign(ReadOnlySpan<byte> message)
        {
            var signer = new MLDsaSigner(_privateKey.Parameters, deterministic: true);
            signer.Init(forSigning: true, _privateKey);
            signer.BlockUpdate(message);
            return signer.GenerateSignature();
        }
    }
}
