// Copyright (C) 2015-2025 The Neo Project.
//
// IWalletProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Wallets
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
