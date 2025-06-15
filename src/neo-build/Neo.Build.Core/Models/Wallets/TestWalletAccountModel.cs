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

using Neo.Build.Core.Json.Converters;
using Neo.Wallets;
using System.Text.Json.Serialization;

namespace Neo.Build.Core.Models.Wallets
{
    public class TestWalletAccountModel : JsonModel
    {
        [JsonConverter(typeof(JsonStringAddressConverter))]
        public UInt160? Address { get; set; }

        public string? Label { get; set; }

        public bool IsDefault { get; set; }

        public bool Lock { get; set; }

        [JsonConverter(typeof(JsonStringKeyPairHexFormatConverter))]
        public KeyPair? Key { get; set; }

        public ContractModel? Contract { get; set; }

        public object? Extra { get; set; }
    }
}
