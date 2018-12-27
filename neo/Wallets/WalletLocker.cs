using System;

namespace Neo.Wallets
{
    internal class WalletLocker : IDisposable
    {
        private Wallet wallet;

        public WalletLocker(Wallet wallet)
        {
            this.wallet = wallet;
        }

        public void Dispose()
        {
            wallet.Lock();
        }
    }
}
