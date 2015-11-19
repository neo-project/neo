using AntShares.Core;
using AntShares.Implementations.Wallets.EntityFramework;
using System.Security;

namespace AntShares.Wallets
{
    internal class MinerWallet : UserWallet
    {
        private MinerWallet(string path, SecureString password, bool create)
            : base(path, password, create)
        {
        }

        public override void AddContract(Contract contract)
        {
        }

        public static MinerWallet Create(string path, SecureString password)
        {
            MinerWallet wallet = new MinerWallet(path, password, true);
            for (int i = 0; i < Blockchain.StandbyMiners.Length; i++)
            {
                wallet.CreateAccount();
            }
            return wallet;
        }

        public static MinerWallet Open(string path, SecureString password)
        {
            return new MinerWallet(path, password, false);
        }
    }
}
