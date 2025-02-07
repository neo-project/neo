// Copyright (C) 2015-2025 The Neo Project.
//
// NeoWallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Neo.Build.Utilities.Core
{
    public class NeoWallet : Task
    {
        [Required]
        public ITaskItem[] Files { get; set; } = [];

        public string? Password { get; set; }

        public byte ProtocolAddressVersion { get; set; }

        [Output]
        public string CommandLine { get; set; } = string.Empty;

        public override bool Execute()
        {
            // add Code
            Log.LogError("Hello World");
            return true;
        }
    }
}
