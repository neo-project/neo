// Copyright (C) 2015-2025 The Neo Project.
//
// BuildProjectModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Exceptions;
using Neo.Build.Models.Wallet;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace Neo.Build.Models
{
    internal class BuildProjectModel : JsonModel
    {
        public Version Version => Version.Parse("1.0");

        [MaybeNull]
        public WalletModel[] Wallets { get; set; }

        public static BuildProjectModel? FromJson(FileInfo file, JsonSerializerOptions? options = default)
        {
            if (file.Exists == false)
                throw new NeoBuildFileNotFoundException(file);

            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;
            var jsonString = File.ReadAllText(file.FullName);

            return FromJson<BuildProjectModel>(jsonString, jsonOptions);
        }

        public override string ToJson(JsonSerializerOptions? options = default) =>
            JsonSerializer.Serialize(this, options ?? _jsonSerializerOptions);

        [return: NotNull]
        public override string? ToString() =>
            ToJson();
    }
}
