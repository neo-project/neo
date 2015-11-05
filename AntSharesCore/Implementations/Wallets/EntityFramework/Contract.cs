namespace AntShares.Implementations.Wallets.EntityFramework
{
    internal class Contract
    {
        public byte[] RedeemScript { get; set; }
        public byte[] ScriptHash { get; set; }
        public byte[] PublicKeyHash { get; set; }
        public Account Account { get; set; }
    }
}
