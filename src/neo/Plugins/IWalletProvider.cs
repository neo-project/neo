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
