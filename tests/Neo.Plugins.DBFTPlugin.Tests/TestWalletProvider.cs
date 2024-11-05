// Copyright (C) 2015-2024 The Neo Project.
//
// TestWalletProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.UnitTests;
using Neo.Wallets;
using System;

namespace Neo.Plugins.DBFTPlugin.Tests;

public class TestWalletProvider(string wif) : IWalletProvider
{
    public event EventHandler<Wallet> WalletChanged;

    private Wallet Wallet
    {
        get
        {
            var wallet = TestUtils.GenerateTestWallet("123");
            var privateKey = Wallet.GetPrivateKeyFromWIF(wif);
            wallet.CreateAccount(privateKey);
            return wallet;
        }
    }

    public Wallet GetWallet() => Wallet;
}
