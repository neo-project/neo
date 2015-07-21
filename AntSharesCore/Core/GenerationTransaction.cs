using System.IO;

namespace AntShares.Core
{
    public class GenerationTransaction : Transaction
    {
        public uint Nonce;

        public GenerationTransaction()
            : base(TransactionType.GenerationTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.Nonce = reader.ReadUInt32();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Nonce);
        }

        internal override bool VerifyBalance()
        {
            return true;
        }
    }
}
