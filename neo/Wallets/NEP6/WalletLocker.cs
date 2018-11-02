using System;

namespace Neo.Wallets.NEP6
{
    internal class WalletLocker : IDisposable
    {
        private NEP6Wallet wallet;

        public WalletLocker(NEP6Wallet wallet)
        {
            this.wallet = wallet;
        }

        public void Dispose()
        {
            wallet.Lock();
        }
    }

    internal class SimpleWalletLocker : IDisposable
    {
        private NEP6SimpleWallet wallet;

        public SimpleWalletLocker(NEP6SimpleWallet wallet)
        {
            this.wallet = wallet;
        }

        public void Dispose()
        {
            wallet.Lock();
        }
    }
}
