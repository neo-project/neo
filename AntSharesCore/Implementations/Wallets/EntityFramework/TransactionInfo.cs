using System;
using CoreTransaction = AntShares.Core.Transaction;

namespace AntShares.Implementations.Wallets.EntityFramework
{
    public class TransactionInfo
    {
        public CoreTransaction Transaction;
        public uint? Height;
        public DateTime Time;
    }
}
