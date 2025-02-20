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
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Neo.Build.Core.Models.Wallets
{
    public class TestWalletModel : JsonModel
    {
        public string? Name { get; set; }

        public Version? Version { get; set; }

        public SCryptModel? Scrypt { get; set; }

        public ICollection<TestWalletAccountModel>? Accounts { get; set; }

        public JsonNode? Extra { get; set; }
    }
}
