// Copyright (C) 2015-2025 The Neo Project.
//
// AccountBalanceModel.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Numerics;

namespace Neo.Build.Core.Models.Wallets
{
    public class AccountBalanceModel
    {
        public UInt160 ContractHash { get; set; } = UInt160.Zero;
        public UInt160 AccountHash { get; set; } = UInt160.Zero;
        public BigInteger Balance { get; set; } = BigInteger.Zero;
        public BigInteger Decimals { get; set; } = BigInteger.Zero;
        public string? Symbol { get; set; } = string.Empty;
    }
}
