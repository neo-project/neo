// Copyright (C) 2015-2024 The Neo Project.
//
// TokenBalanceModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Numerics;

namespace Neo.Plugins.RestServer.Models.Token
{
    public class TokenBalanceModel
    {
        public string Name { get; set; } = string.Empty;
        public UInt160 ScriptHash { get; set; } = UInt160.Zero;
        public string Symbol { get; set; } = string.Empty;
        public byte Decimals { get; set; }
        public BigInteger Balance { get; set; }
        public BigInteger TotalSupply { get; set; }
    }
}
