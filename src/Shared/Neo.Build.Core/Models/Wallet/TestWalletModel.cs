// Copyright (C) 2015-2025 The Neo Project.
//
// TestWalletModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Text.Json;

namespace Neo.Build.Core.Models.Wallet
{
    public class TestWalletModel : JsonModel
    {
        public Version? Version { get; set; }

        public SCryptModel? SCrypt { get; set; }

        public string? Name { get; set; }

        public static TestWalletModel? FromJson(string jsonString, JsonSerializerOptions? options = default)
        {
            var jsonOptions = options ?? NeoBuildDefaults.JsonDefaultSerializerOptions;

            return FromJson<TestWalletModel>(jsonString, jsonOptions);
        }
    }
}
