// Copyright (C) 2015-2025 The Neo Project.
//
// ContractModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Models.Wallet;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Contract = Neo.SmartContract.Contract;
using ContractParameterType = Neo.SmartContract.ContractParameterType;

namespace Neo.Build.Models
{
    internal class ContractModel : JsonModel
    {
        [MaybeNull]
        public byte[] Script { get; set; }

        [MaybeNull]
        public ContractParameterType[] Parameters { get; set; }

        public static SCryptParametersModel? FromJson(
            [DisallowNull][StringSyntax(StringSyntaxAttribute.Json)] string jsonString,
            JsonSerializerOptions? options = default)
        {
            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            return FromJson<SCryptParametersModel>(jsonString, jsonOptions);
        }

        [return: NotNull]
        public override string ToJson(JsonSerializerOptions? options = default) =>
            JsonSerializer.Serialize(this, options ?? _jsonSerializerOptions);

        [return: NotNull]
        public override string? ToString() =>
            ToJson();

        public Contract ToObject() =>
            Contract.Create(Parameters, Script);
    }
}
