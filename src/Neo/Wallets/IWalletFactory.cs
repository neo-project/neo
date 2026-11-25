// Copyright (C) 2015-2026 The Neo Project.
//
// IWalletFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Wallets;

public interface IWalletFactory
{
    /// <summary>
    /// Determines whether the factory can handle the specified path.
    /// </summary>
    /// <param name="path">The path of the wallet file.</param>
    /// <returns>
    /// <see langword="true"/> if the factory can handle the specified path; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Handle(string path);

    /// <summary>
    /// Creates a new wallet.
    /// </summary>
    /// <param name="name">The name of the wallet.</param>
    /// <param name="path">The path of the wallet file.</param>
    /// <param name="password">The password of the wallet.</param>
    /// <param name="settings">The settings of the wallet.</param>
    public Wallet CreateWallet(string? name, string path, string password, ProtocolSettings settings);

    /// <summary>
    /// Opens a wallet.
    /// </summary>
    /// <param name="path">The path of the wallet file.</param>
    /// <param name="password">The password of the wallet. The wallet is opened in read-only mode if the password is <see langword="null"/>.</param>
    /// <param name="settings">The settings of the wallet.</param>
    public Wallet OpenWallet(string path, string? password, ProtocolSettings settings);
}
