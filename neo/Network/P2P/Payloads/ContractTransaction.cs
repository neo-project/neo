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
            if (Version > 2) throw new FormatException();
        }
    }
}
