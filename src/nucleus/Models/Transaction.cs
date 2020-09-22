using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Models
{
    public class Transaction 
    {
        public const int MaxTransactionSize = 102400;
        public const uint MaxValidUntilBlockIncrement = 2102400;
        public const int MaxTransactionAttributes = 16;

        public byte Version;
        public uint Nonce;
        public long SystemFee;
        public long NetworkFee;
        public uint ValidUntilBlock;
        public Signer[] Signers;
        public TransactionAttribute[] Attributes;
        public byte[] Script;
        public Witness[] Witnesses;
    }
}
