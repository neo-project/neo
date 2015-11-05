namespace AntShares.Implementations.Wallets.EntityFramework
{
    internal class UnspentCoin
    {
        public byte[] TxId { get; set; }
        public short _Index { get; set; }
        public byte[] AssetId { get; set; }
        public long Value { get; set; }
        public byte[] ScriptHash { get; set; }

        public ushort Index
        {
            get
            {
                return (ushort)_Index;
            }
            set
            {
                _Index = (short)value;
            }
        }
    }
}
