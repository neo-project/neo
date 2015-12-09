using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
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

        public void Sign(Block block, ECPoint[] miners)
        {
            SignatureContext context = new SignatureContext(block);
            Contract contract = MultiSigContract.Create(null, miners.Length / 2 + 1, miners);
            foreach (ECPoint pubKey in miners)
            {
                UInt160 publicKeyHash = pubKey.EncodePoint(true).ToScriptHash();
                Account account = GetAccount(publicKeyHash);
                if (account == null) continue;
                byte[] signature = block.Sign(account);
                context.Add(contract, account.PublicKey, signature);
            }
            block.Script = context.GetScripts()[0];
        }
    }
}
