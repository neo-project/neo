using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class ContractTransaction : Transaction
    {
        public ContractTransaction()
            : base(TransactionType.ContractTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            // 0: old default
            // 1: invalid
            // 2: includes BlockHeight
            if (Version > 2) throw new FormatException();
            if (Version == 1) throw new FormatException();
        }
    }
}
