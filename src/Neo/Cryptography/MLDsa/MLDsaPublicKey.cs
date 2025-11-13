// Copyright (C) 2015-2025 The Neo Project.
//
// MLDsaPublicKey.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using System;

namespace Neo.Cryptography
{
    public readonly struct MLDsaPublicKey
    {
        private readonly MLDsaPublicKeyParameters _publicKey;

        internal MLDsaPublicKey(MLDsaPublicKeyParameters publicKey)
        {
            _publicKey = publicKey;
        }

        /// <summary>
        /// Exports the public key of the MLDsa65 with the FIPS 204 public key format.
        /// </summary>
        /// <returns>The public key in the FIPS 204 public key format.</returns>
        public readonly byte[] ExportPublicKey() => _publicKey.GetEncoded();

        /// <summary>
        /// Imports the public key of the MLDsa65 with the FIPS 204 key format.
        /// </summary>
        /// <param name="publicKey">The public key in the FIPS 204 key format.</param>
        /// <returns>The MLDsa65 public key imported from the public key.</returns>
        /// <exception cref="ArgumentException">The public key is invalid.</exception>
        public static MLDsaPublicKey ImportMLDsa65PublicKey(byte[] publicKey)
        {
            var key = MLDsaPublicKeyParameters.FromEncoding(MLDsaParameters.ml_dsa_65, publicKey);
            return new MLDsaPublicKey(key);
        }

        /// <summary>
        /// Verifies the specified message with the signature of the MLDsa65.
        /// </summary>
        /// <param name="message">The message to verify.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>True if the message is verified, false otherwise.</returns>
        public readonly bool Verify(ReadOnlySpan<byte> message, byte[] signature)
        {
            var verifier = new MLDsaSigner(_publicKey.Parameters, deterministic: true);
            verifier.Init(forSigning: false, _publicKey);
            verifier.BlockUpdate(message);
            return verifier.VerifySignature(signature);
        }
    }
}
