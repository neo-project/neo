using System;
using System.Threading;

namespace Neo.Wallets
{
    public class WalletLocker : IDisposable
    {
        private Wallet wallet;
        private Timer timer;
        private DateTime unlockTime;
        private int duration;

        public WalletLocker(Wallet wallet)
        {
            this.wallet = wallet;
        }

        public WalletLocker(Wallet wallet, uint second)
            :this(wallet)
        {
            Unlock(second);
        }

        public void Unlock(uint second)
        {
            if (timer == null)
                timer = new Timer(new TimerCallback(wallet.Lock), null, 1000 * second, -1);
            else
            {
                if (DateTime.Now.AddSeconds(second) > unlockTime.AddSeconds(duration))
                    timer.Change(1000 * second, -1);
            }
        }

        public void Lock()
        {
            wallet.Lock();
        }

        public void Dispose()
        {
            wallet.Lock();
            timer.Dispose();
        }
    }
}
