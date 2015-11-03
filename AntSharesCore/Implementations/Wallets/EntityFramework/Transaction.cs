using AntShares.Core;

namespace AntShares.Implementations.Wallets.EntityFramework
{
    internal class Transaction
    {
        public byte[] Hash { get; set; }
        public TransactionType Type { get; set; }
        public byte[] RawData { get; set; }
    }
}
