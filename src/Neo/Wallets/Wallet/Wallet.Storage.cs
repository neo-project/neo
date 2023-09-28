// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Wallets.NEP6;
using System;

namespace Neo.Wallets;

/// <summary>
/// The base class of wallets.
/// </summary>
public abstract partial class Wallet
{
    /// <summary>
    /// Saves the wallet file to the disk. It uses the value of <see cref="Path"/> property.
    /// </summary>
    public abstract void Save();

    public static Wallet Open(string path, string password, ProtocolSettings settings)
    {
        return GetFactory(path)?.OpenWallet(path, password, settings);
    }

    /// <summary>
    /// Migrates the accounts from old wallet to a new <see cref="NEP6Wallet"/>.
    /// </summary>
    /// <param name="password">The password of the wallets.</param>
    /// <param name="path">The path of the new wallet file.</param>
    /// <param name="oldPath">The path of the old wallet file.</param>
    /// <param name="settings">The <see cref="ProtocolSettings"/> to be used by the wallet.</param>
    /// <returns>The created new wallet.</returns>
    public static Wallet Migrate(string path, string oldPath, string password, ProtocolSettings settings)
    {
        IWalletFactory factoryOld = GetFactory(oldPath);
        if (factoryOld is null)
            throw new InvalidOperationException("The old wallet file format is not supported.");
        IWalletFactory factoryNew = GetFactory(path);
        if (factoryNew is null)
            throw new InvalidOperationException("The new wallet file format is not supported.");

        Wallet oldWallet = factoryOld.OpenWallet(oldPath, password, settings);
        Wallet newWallet = factoryNew.CreateWallet(oldWallet.Name, path, password, settings);

        foreach (WalletAccount account in oldWallet.GetAccounts())
        {
            newWallet.CreateAccount(account.Contract, account.GetKey());
        }
        return newWallet;
    }


}
