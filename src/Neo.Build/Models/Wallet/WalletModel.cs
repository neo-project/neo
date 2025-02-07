// Copyright (C) 2015-2025 The Neo Project.
//
// WalletModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Exceptions.Wallet;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace Neo.Build.Models.Wallet
{
    internal class WalletModel : JsonModel
    {
        [MaybeNull]
        public string Name { get; set; }

        [NotNull]
        public SCryptParametersModel SCrypt { get; set; } = SCryptParametersModel.Default;

        [MaybeNull]
        public WalletAccountModel Accounts { get; set; }


        public static WalletModel? FromJson(
            [DisallowNull][StringSyntax(StringSyntaxAttribute.Json)] string jsonString,
            JsonSerializerOptions? options = default)
        {
            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            return FromJson<WalletModel>(jsonString, jsonOptions);
        }

        public static WalletModel? FromJson([DisallowNull] FileInfo file, JsonSerializerOptions? options = default)
        {
            if (file.Exists == false)
                throw new WalletFileNotFoundException(file);

            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;
            var jsonString = File.ReadAllText(file.FullName);

            return FromJson<WalletModel>(jsonString, jsonOptions);
        }

        [return: NotNull]
        public override string ToJson(JsonSerializerOptions? options = default) =>
            JsonSerializer.Serialize(this, options ?? _jsonSerializerOptions);

        [return: NotNull]
        public override string? ToString() =>
            ToJson();
    }
}
