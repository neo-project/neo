// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Wallets.SQLite
{
    internal class Contract
    {
        public byte[] RawData { get; set; }
        public byte[] ScriptHash { get; set; }
        public byte[] PublicKeyHash { get; set; }
        public Account Account { get; set; }
        public Address Address { get; set; }
    }
}
