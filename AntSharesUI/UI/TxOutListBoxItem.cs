using AntShares.Wallets;

namespace AntShares.UI
{
    internal class TxOutListBoxItem
    {
        public UInt160 Account;
        public Fixed8 Amount;

        public override string ToString()
        {
            return string.Format("{0}\t{1}", Wallet.ToAddress(Account), Amount);
        }
    }
}
