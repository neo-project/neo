// Copyright (C) 2015-2025 The Neo Project.
//
// WalletConfigurationModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Build.Models.Wallet
{
    internal class WalletConfigurationModel : JsonModel
    {
        [NotNull]
        [JsonPropertyOrder(-1)]
        public byte AddressVersion { get; set; }

        [MaybeNull]
        [JsonPropertyOrder(0)]
        public string Name { get; set; }

        [MaybeNull]
        [JsonPropertyOrder(1)]
        public string Password { get; set; }

        public static WalletConfigurationModel? FromJson(FileInfo file, JsonSerializerOptions? options = default)
        {
            if (file.Exists == false)
                throw new WalletConfigFileNotFoundException(file);

            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;
            var jsonString = File.ReadAllText(file.FullName);

            if (string.IsNullOrEmpty(jsonString))
                throw new WalletConfigFileNotFoundException(file);

            return FromJson<WalletConfigurationModel>(jsonString, jsonOptions);
        }

        public override string ToJson() =>
            JsonSerializer.Serialize(this, options: _jsonSerializerOptions);

        public override string? ToString() =>
            ToJson();
    }
}
