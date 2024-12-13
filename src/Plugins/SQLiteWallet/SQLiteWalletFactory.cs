// Copyright (C) 2015-2024 The Neo Project.
//
// SQLiteWalletFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins;
using static System.IO.Path;

namespace Neo.Wallets.SQLite
{
    public class SQLiteWalletFactory : Plugin, IWalletFactory
    {
        public override string Name => "SQLiteWallet";
        public override string Description => "A SQLite-based wallet provider that supports wallet files with .db3 suffix.";

        public SQLiteWalletFactory()
        {
            Wallet.RegisterFactory(this);
        }

        public bool Handle(string path)
        {
            return GetExtension(path).ToLowerInvariant() == ".db3";
        }

        public Wallet CreateWallet(string name, string path, string password, ProtocolSettings settings)
        {
            return SQLiteWallet.Create(path, password, settings);
        }

        public Wallet OpenWallet(string path, string password, ProtocolSettings settings)
        {
            return SQLiteWallet.Open(path, password, settings);
        }
    }
}
