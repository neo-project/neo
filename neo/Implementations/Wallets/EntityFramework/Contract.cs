namespace Neo.Implementations.Wallets.EntityFramework
{
    internal class Contract
    {
        public byte[] RawData { get; set; }
        public byte[] ScriptHash { get; set; }
        public byte[] PublicKeyHash { get; set; }
        public Account Account { get; set; }
        public Address Address { get; set; }
    }
}
