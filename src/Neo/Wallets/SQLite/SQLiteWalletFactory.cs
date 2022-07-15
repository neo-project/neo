// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins;
using System.IO;

namespace Neo.Wallets.SQLite
{
    class SQLiteWalletFactory : IWalletFactory
    {
        public static readonly SQLiteWalletFactory Instance = new();

        public bool Handle(string filename)
        {
            return Path.GetExtension(filename).ToLowerInvariant() == ".db3";
        }

        public Wallet CreateWallet(string name, string path, string password, ProtocolSettings settings)
        {
            return UserWallet.Create(path, password, settings);
        }

        public Wallet OpenWallet(string path, string password, ProtocolSettings settings)
        {
            return UserWallet.Open(path, password, settings);
        }
    }
}
