// Copyright (C) 2015-2025 The Neo Project.
//
// KeyPairHelper.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Extensions;
using Neo.Wallets;
using System;
using System.Security;
using System.Security.Cryptography;

namespace Neo.Build.Core.Cryptography
{
    public static class KeyPairHelper
    {
        public static KeyPair CreateNew()
        {
            var bytes = new byte[32];

            RandomNumberGenerator.Fill(bytes);

            return new(bytes);
        }

        public static SecureString ToWifString(ReadOnlySpan<byte> privateKey)
        {
            Span<byte> data = [0x80, .. privateKey, 0x01];
            var wif = Base58.Base58CheckEncode(data).ToSecureString();

            data.Clear();

            return wif;
        }

        public static SecureString ToWifString(KeyPair key) =>
            ToWifString(key.PrivateKey);
    }
}
