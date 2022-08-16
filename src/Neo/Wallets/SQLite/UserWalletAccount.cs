// Copyright (C) 2015-2022 The Neo Project.
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
    sealed class UserWalletAccount : WalletAccount
    {
        public KeyPair Key;

        public override bool HasKey => Key != null;

        public UserWalletAccount(UInt160 scriptHash, ProtocolSettings settings)
            : base(scriptHash, settings)
        {
        }

        public override KeyPair GetKey()
        {
            return Key;
        }
    }
}
