using AntShares.IO;
using System.IO;

namespace AntShares.Core
{
    public class ContractTransaction : Transaction
    {
        public TransactionAttribute[] Attributes;

        public ContractTransaction()
            : base(TransactionType.ContractTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.Attributes = reader.ReadSerializableArray<TransactionAttribute>();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Attributes);
        }
    }
}
