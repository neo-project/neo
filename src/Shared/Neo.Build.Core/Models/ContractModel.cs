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

using Neo.Build.Core.Interfaces;
using Neo.SmartContract;
using System.Text.Json;

namespace Neo.Build.Core.Models
{
    public class ContractModel : JsonModel, IConvertToObject<Contract>
    {
        public byte[]? Script { get; set; }

        public ContractParameterType[]? Parameters { get; set; }

        public static ContractModel? FromJson(string jsonString, JsonSerializerOptions? options = default)
        {
            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            return FromJson<ContractModel>(jsonString, jsonOptions);
        }

        public Contract ToObject() =>
            Contract.Create(Parameters, Script);
    }
}
