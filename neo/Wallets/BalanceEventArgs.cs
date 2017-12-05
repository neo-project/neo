using Neo.Core;
using System;

namespace Neo.Wallets
{
    public class BalanceEventArgs : EventArgs
    {
        public Transaction Transaction;
        public UInt160[] RelatedAccounts;
        public uint? Height;
        public uint Time;
    }
}
