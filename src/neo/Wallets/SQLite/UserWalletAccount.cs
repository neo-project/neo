namespace Neo.Wallets.SQLite
{
    sealed class UserWalletAccount : WalletAccount
    {
        public KeyPair Key;

        public override bool HasKey => Key != null;

        public UserWalletAccount(UInt160 scriptHash, ProtocolSettings settings)
            : base(scriptHash, settings)
        {
        }

        public override KeyPair GetKey()
        {
            return Key;
        }
    }
}
