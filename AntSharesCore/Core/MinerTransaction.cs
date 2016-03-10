using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class MinerTransaction : Transaction
    {
        public uint Nonce;

        public MinerTransaction()
            : base(TransactionType.MinerTransaction)
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
            if (Outputs.Any(p => p.AssetId != Blockchain.AntCoin.Hash))
                throw new FormatException();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Nonce);
        }
    }
}
