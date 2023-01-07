// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;

namespace Neo.Wallets.NEP6
{
    class NEP6WalletFactory : IWalletFactory
    {
        public static readonly NEP6WalletFactory Instance = new();

        public bool Handle(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() == ".json";
        }

        public Wallet CreateWallet(string name, string path, string password, ProtocolSettings settings)
        {
            if (File.Exists(path))
                throw new InvalidOperationException("The wallet file already exists.");
            NEP6Wallet wallet = new NEP6Wallet(path, password, settings, name);
            wallet.Save();
            return wallet;
        }

        public Wallet OpenWallet(string path, string password, ProtocolSettings settings)
        {
            return new NEP6Wallet(path, password, settings);
        }
    }
}
