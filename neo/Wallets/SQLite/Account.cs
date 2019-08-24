namespace Neo.Wallets.SQLite
{
    internal class Account
    {
        public byte[] PrivateKeyEncrypted { get; set; }
        public byte[] PublicKeyHash { get; set; }
    }
}
