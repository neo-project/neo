// Copyright (C) 2015-2025 The Neo Project.
//
// SCryptParametersModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Models.Interfaces;
using Neo.Wallets.NEP6;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Neo.Build.Models.Wallet
{
    internal class SCryptParametersModel : JsonModel, IConvertNeoType<ScryptParameters>
    {
        public static readonly SCryptParametersModel Default = new();

        /// <summary>
        /// CPU/Memory cost parameter. Must be larger than 1, a power of 2 and less than 2^(128 * r / 8).
        /// </summary>
        public int N { get; set; }

        /// <summary>
        /// The block size, must be >= 1.
        /// </summary>
        public int R { get; set; }

        /// <summary>
        /// Parallelization parameter. Must be a positive integer less than or equal to Int32.MaxValue / (128 * r * 8).
        /// </summary>
        public int P { get; set; }

        public static SCryptParametersModel? FromJson(
            [DisallowNull][StringSyntax(StringSyntaxAttribute.Json)] string jsonString,
            JsonSerializerOptions? options = default)
        {
            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            return FromJson<SCryptParametersModel>(jsonString, jsonOptions);
        }

        public override string ToJson(JsonSerializerOptions? options = default) =>
            JsonSerializer.Serialize(this, options ?? _jsonSerializerOptions);

        [return: NotNull]
        public override string? ToString() =>
            ToJson();

        public ScryptParameters ToObject() =>
            new(N, R, P);
    }
}
