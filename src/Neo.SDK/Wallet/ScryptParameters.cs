// Copyright (C) 2015-2024 The Neo Project.
//
// ScryptParameters.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;

namespace Neo.SDK.Wallet
{
    public class ScryptParameters
    {
        public static ScryptParameters Default => new() { N = 16384, R = 8, P = 8 };

        public int N { get; set; }
        public int R { get; set; }
        public int P { get; set; }

        public static ScryptParameters Load(IConfigurationSection section) =>
            new()
            {
                N = section.GetValue<int>("n"),
                R = section.GetValue<int>("r"),
                P = section.GetValue<int>("p")
            };
    }
}
