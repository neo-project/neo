// Copyright (C) 2015-2025 The Neo Project.
//
// WalletAccountModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Wallets;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Contract = Neo.SmartContract.Contract;

namespace Neo.Build.Models.Wallet
{
    internal class WalletAccountModel : JsonModel
    {
        [MaybeNull]
        public string Name { get; set; }

        [MaybeNull]
        public bool IsDefault { get; set; }

        [MaybeNull]
        public UInt160 ScriptHash { get; set; }

        [MaybeNull]
        public KeyPair Key { get; set; }

        [MaybeNull]
        public Contract Contract { get; set; }

        public static WalletAccountModel? FromJson(
            [DisallowNull][StringSyntax(StringSyntaxAttribute.Json)] string jsonString,
            JsonSerializerOptions? options = default)
        {
            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            return FromJson<WalletAccountModel>(jsonString, jsonOptions);
        }

        [return: NotNull]
        public override string ToJson(JsonSerializerOptions? options = default) =>
            JsonSerializer.Serialize(this, options ?? _jsonSerializerOptions);

        [return: NotNull]
        public override string? ToString() =>
            ToJson();
    }
}
