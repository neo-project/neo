// Copyright (C) 2015-2025 The Neo Project.
//
// TestWalletAccountModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Wallets;
using System.Text.Json;

namespace Neo.Build.Core.Models.Wallet
{
    public class TestWalletAccountModel : JsonModel
    {
        public string? Label { get; set; }

        public bool IsDefault { get; set; }

        public UInt160? ScriptHash { get; set; }

        public KeyPair? Key { get; set; }

        public ContractModel? Contract { get; set; }

        public static TestWalletAccountModel? FromJson(string jsonString, JsonSerializerOptions? options = default)
        {
            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            return FromJson<TestWalletAccountModel>(jsonString, jsonOptions);
        }
    }
}
