// Copyright (C) 2015-2024 The Neo Project.
//
// WalletServiceProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Service.App.Extensions;
using Neo.Wallets;
using System.Collections.Concurrent;
using System.IO;
using System.Security;

namespace Neo.Service.App.Providers
{
    internal sealed class WalletServiceProvider
    {

        private readonly ProtocolSettings _protocolSettings;
        private readonly ConcurrentDictionary<string, Wallet> _openWallets = new();

        public WalletServiceProvider() : this(ProtocolSettings.Default)
        {
        }

        public WalletServiceProvider(
            ProtocolSettings protocolSettings)
        {
            _protocolSettings = protocolSettings;
        }

        public WalletServiceProvider(
            IConfiguration config)
        {
            _protocolSettings = ProtocolSettings.Load(config.GetRequiredSection("ProtocolConfiguration"));

        }

        public void AddJsonFile(FileInfo file, SecureString password)
        {
            if (file.Exists == false)
                throw new FileNotFoundException(file.FullName);

            var wallet = Wallet.Open(file.FullName, password.GetValue(), _protocolSettings) ??
                throw new FileLoadException("Wallet information is correct.", file.FullName);

            var walletName = wallet.Name;
            if (string.IsNullOrEmpty(walletName))
                walletName = wallet.GetDefaultAccount().Address;

            _ = _openWallets.TryAdd(walletName, wallet);
        }

        public bool TryGetWallet(string walletName, out Wallet? wallet) =>
            _openWallets.TryGetValue(walletName, out wallet);

        public Wallet? GetWallet(string walletName)
        {
            if (_openWallets.IsEmpty)
                return null;

            if (_openWallets.TryGetValue(walletName, out var wallet))
                return wallet;

            return null;
        }
    }
}
