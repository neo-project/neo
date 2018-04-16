using Neo.Wallets;

namespace Neo.Implementations.Wallets.EntityFramework
{
    internal class UserWalletAccount : WalletAccount
    {
        public KeyPair Key;

        public override bool HasKey => Key != null;

        public UserWalletAccount(UInt160 scriptHash)
            : base(scriptHash)
        {
        }

        public override KeyPair GetKey()
        {
            return Key;
        }
    }
}
