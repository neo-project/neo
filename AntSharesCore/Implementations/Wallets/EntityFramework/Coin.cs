using AntShares.Wallets;

namespace AntShares.Implementations.Wallets.EntityFramework
{
    internal class Coin
    {
        public byte[] TxId { get; set; }
        public ushort Index { get; set; }
        public byte[] AssetId { get; set; }
        public long Value { get; set; }
        public byte[] ScriptHash { get; set; }
        public CoinState State { get; set; }
        public Contract Contract { get; set; }
    }
}
