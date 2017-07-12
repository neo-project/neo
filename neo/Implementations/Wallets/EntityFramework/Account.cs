namespace Neo.Implementations.Wallets.EntityFramework
{
    internal class Account
    {
        public byte[] PrivateKeyEncrypted { get; set; }
        public byte[] PublicKeyHash { get; set; }
    }
}
