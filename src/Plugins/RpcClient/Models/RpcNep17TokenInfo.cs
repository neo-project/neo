// Copyright (C) 2015-2025 The Neo Project.
//
// RpcNep17TokenInfo.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Numerics;

namespace Neo.Network.RPC.Models
{
    public class RpcNep17TokenInfo
    {
        public string Name { get; set; }

        public string Symbol { get; set; }

        public byte Decimals { get; set; }

        public BigInteger TotalSupply { get; set; }
    }
}
