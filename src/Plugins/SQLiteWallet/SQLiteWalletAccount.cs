// Copyright (C) 2015-2024 The Neo Project.
//
// SQLiteWalletAccount.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Wallets.SQLite
{
    sealed class SQLiteWalletAccount : WalletAccount
    {
        public KeyPair Key;

        public override bool HasKey => Key != null;

        public SQLiteWalletAccount(UInt160 scriptHash, ProtocolSettings settings)
            : base(scriptHash, settings)
        {
        }

        public override KeyPair GetKey()
        {
            return Key;
        }
    }
}
