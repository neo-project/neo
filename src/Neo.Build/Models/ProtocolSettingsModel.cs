// Copyright (C) 2015-2025 The Neo Project.
//
// ProtocolSettingsModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Models.Interfaces;
using Neo.Cryptography.ECC;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Neo.Build.Models
{
    internal class ProtocolSettingsModel : JsonModel, IConvertNeoType<ProtocolSettings>
    {
        public uint Network { get; set; }

        public byte AddressVersion { get; set; }

        public uint MillisecondsPerBlock { get; set; }

        public uint MaxTransactionsPerBlock { get; set; }

        public int MemoryPoolMaxTransactions { get; set; }

        public uint MaxTraceableBlocks { get; set; }

        [MaybeNull]
        public IReadOnlyDictionary<Hardfork, uint> HardForks { get; set; }

        public ulong InitialGasDistribution { get; set; }

        public int ValidatorsCount { get; set; }

        [MaybeNull]
        public ECPoint[] StandbyCommittee { get; set; }

        [MaybeNull]
        public string[] SeedList { get; set; }


        public static ProtocolSettingsModel? FromJson(
            [DisallowNull][StringSyntax(StringSyntaxAttribute.Json)] string jsonString,
            JsonSerializerOptions? options = default)
        {
            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            return FromJson<ProtocolSettingsModel>(jsonString, jsonOptions);
        }

        public override string ToJson(JsonSerializerOptions? options = default) =>
            JsonSerializer.Serialize(this, options ?? _jsonSerializerOptions);

        [return: NotNull]
        public override string? ToString() =>
            ToJson();

        public ProtocolSettings ToObject() =>
            ProtocolSettings.Default with
            {
                Network = Network,
                AddressVersion = AddressVersion,
                MillisecondsPerBlock = MillisecondsPerBlock,
                MaxTransactionsPerBlock = MaxTransactionsPerBlock,
                MemoryPoolMaxTransactions = MemoryPoolMaxTransactions,
                MaxTraceableBlocks = MaxTraceableBlocks,
                InitialGasDistribution = InitialGasDistribution,
                ValidatorsCount = ValidatorsCount,
                StandbyCommittee = StandbyCommittee,
                Hardforks = HardForks?.ToImmutableDictionary(),
                SeedList = SeedList,
            };
    }
}
