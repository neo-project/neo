// Copyright (C) 2015-2026 The Neo Project.
//
// AccountManagement.Keys.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051
using Neo.Cryptography.ECC;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract.Native
{
    partial class AccountManagement
    {
        private sealed class P256PublicKeyAttribute : ValidatorAttribute
        {
            public override void Validate(StackItem item)
            {
                if (item is not ByteString)
                    throw new FormatException("A P-256 public key must be an exact ByteString.");
            }
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.None)]
        private static byte[] CanonicalP256PublicKey([P256PublicKey] byte[] publicKey)
        {
            byte[] compressed;
            if (publicKey.Length == 33 && publicKey[0] is 2 or 3)
                compressed = publicKey;
            else if (publicKey.Length == 65 && publicKey[0] == 4)
            {
                compressed = new byte[33];
                compressed[0] = (byte)(2 | (publicKey[64] & 1));
                publicKey.AsSpan(1, 32).CopyTo(compressed.AsSpan(1));
            }
            else throw new FormatException("A canonical P-256 public key requires a 33-byte compressed or 65-byte uncompressed encoding.");

            // Compressed decoding checks the field and square root. Do not decode
            // the uncompressed input directly: that parser preserves unchecked Y.
            var point = ECPoint.DecodePoint(compressed, ECCurve.Secp256r1);
            if (publicKey.Length == 65 && !publicKey.AsSpan().SequenceEqual(point.EncodePoint(false)))
                throw new FormatException("The uncompressed P-256 public key is not a point on the curve.");
            // ECPoint caches its encoded arrays; never expose the cache's backing bytes.
            return point.EncodePoint(true).AsSpan().ToArray();
        }
    }
}
