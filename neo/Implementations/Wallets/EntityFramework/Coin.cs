using Neo.Core;

namespace Neo.Implementations.Wallets.EntityFramework
{
    internal class Coin
    {
        public byte[] TxId { get; set; }
        public ushort Index { get; set; }
        public byte[] AssetId { get; set; }
        public long Value { get; set; }
        public byte[] ScriptHash { get; set; }
        public CoinState State { get; set; }
        public Address Address { get; set; }
    }
}
