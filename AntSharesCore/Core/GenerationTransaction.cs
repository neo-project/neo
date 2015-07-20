using System;
using System.IO;

namespace AntShares.Core
{
    public class GenerationTransaction : Transaction
    {
        public UInt32 Nonce;

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

        public override bool Verify()
        {
            //TODO: 验证合法性
        }
    }
}
