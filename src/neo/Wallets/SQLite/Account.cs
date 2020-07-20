namespace Neo.Wallets.SQLite
{
    internal class Account
    {
        public byte[] PublicKeyHash { get; set; }
        public string Nep2key { get; set; }
    }
}
