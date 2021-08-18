// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Wallets;
using System;

namespace Neo.Plugins
{
    /// <summary>
    /// A provider for obtaining wallet instance.
    /// </summary>
    public interface IWalletProvider
    {
        /// <summary>
        /// Triggered when a wallet is opened or closed.
        /// </summary>
        event EventHandler<Wallet> WalletChanged;

        /// <summary>
        /// Get the currently opened <see cref="Wallet"/> instance.
        /// </summary>
        /// <returns>The opened wallet. Or <see langword="null"/> if no wallet is opened.</returns>
        Wallet GetWallet();
    }
}
