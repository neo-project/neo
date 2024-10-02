// Copyright (C) 2015-2024 The Neo Project.
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
using System;
using System.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace Neo.Cryptography;

public class Ed25519
{
    internal const int PublicKeySize = 32;
    private const int PrivateKeySize = 32;
    internal const int SignatureSize = 64;

    public static byte[] GenerateKeyPair()
    {
        var keyPairGenerator = new Ed25519KeyPairGenerator();
        keyPairGenerator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        var keyPair = keyPairGenerator.GenerateKeyPair();
        return ((Ed25519PrivateKeyParameters)keyPair.Private).GetEncoded();
    }

    public static byte[] GetPublicKey(byte[] privateKey)
    {
        if (privateKey.Length != PrivateKeySize)
            throw new ArgumentException("Invalid private key size", nameof(privateKey));

        var privateKeyParams = new Ed25519PrivateKeyParameters(privateKey, 0);
        return privateKeyParams.GeneratePublicKey().GetEncoded();
    }

    public static byte[] Sign(byte[] message, byte[] privateKey)
    {
        if (privateKey.Length != PrivateKeySize)
            throw new ArgumentException("Invalid private key size", nameof(privateKey));

        var signer = new Ed25519Signer();
        signer.Init(true, new Ed25519PrivateKeyParameters(privateKey, 0));
        signer.BlockUpdate(message, 0, message.Length);
        return signer.GenerateSignature();
    }

    public static bool Verify(byte[] message, byte[] signature, byte[] publicKey)
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
