using System;
using CoreTransaction = Neo.Core.Transaction;

namespace Neo.Implementations.Wallets.EntityFramework
{
    public class TransactionInfo
    {
        public CoreTransaction Transaction;
        public uint? Height;
        public DateTime Time;
    }
}
