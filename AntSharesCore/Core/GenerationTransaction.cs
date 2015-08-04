using System;
using System.IO;
using System.Linq;

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

        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            if (Inputs.Length != 0)
                throw new FormatException();
            if (Outputs.Any(p => p.AssetId != Blockchain.AntCoin.Hash || p.Value <= Fixed8.Zero))
                throw new FormatException();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Nonce);
        }
    }
}
