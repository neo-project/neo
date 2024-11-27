// Copyright (C) 2015-2024 The Neo Project.
//
// IWalletChangedHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Wallets;

namespace Neo.IEventHandlers
{
    public interface IWalletChangedHandler
    {
        /// <summary>
        /// The handler of WalletChanged event from the <see cref="IWalletProvider"/>.
        /// Triggered when a new wallet is assigned to the node.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="wallet">The new wallet being assigned to the system.</param>
        void IWalletProvider_WalletChanged_Handler(object sender, Wallet wallet);
    }
}
