// Copyright (C) 2015-2025 The Neo Project.
//
// TestWalletAccount.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Cryptography;
using Neo.Wallets;
using System.Text.Json;

namespace Neo.Build.Core.Models.Wallet
{
    public class TestWalletAccount : JsonModel
    {
        public string? Name { get; set; }

        public bool IsDefault { get; set; }

        public UInt160 ScriptHash => Key.PublicKeyHash;

        public KeyPair Key { get; set; } = KeyPairGenerator.CreateNew();

        public ContractModel Contract { get; set; } = new();

        public static TestWalletAccount? FromJson(string jsonString, JsonSerializerOptions? options = default)
        {
            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            return FromJson<TestWalletAccount>(jsonString, jsonOptions);
        }
    }
}
