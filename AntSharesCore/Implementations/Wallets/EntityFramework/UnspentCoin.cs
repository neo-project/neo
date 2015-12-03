namespace AntShares.Implementations.Wallets.EntityFramework
{
    internal class UnspentCoin
    {
        public byte[] TxId { get; set; }
        public ushort Index { get; set; }
        public byte[] AssetId { get; set; }
        public long Value { get; set; }
        public byte[] ScriptHash { get; set; }
        public bool IsChange { get; set; }
        public Contract Contract { get; set; }
    }
}
