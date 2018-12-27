using System;

namespace Neo.Wallets.SQLite
{
    internal class UserWalletAccount : WalletAccount
    {
        private readonly UserWallet wallet;
        public KeyPair Key;
        public byte[] EncryptedPrivateKey;

        public override bool HasKey => EncryptedPrivateKey != null;

        public UserWalletAccount(UserWallet wallet, UInt160 scriptHash)
            : base(scriptHash)
        {
            this.wallet = wallet;
        }

        public override KeyPair GetKey()
        {
            if (EncryptedPrivateKey == null) return null;
            if (Key == null)
            {
                try
                {
                    Key = new KeyPair(wallet.DecryptPrivateKey(EncryptedPrivateKey));
                }
                catch (ArgumentNullException)
                {
                    return null;
                }
            }
            return Key;
        }
    }
}
