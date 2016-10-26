using AntShares.IO;
using AntShares.IO.Json;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class PublishTransaction : Transaction
    {
        public byte[][] Contracts;

        public override int Size => base.Size + Contracts.Length.GetVarSize() + Contracts.Sum(p => p.Length.GetVarSize() + p.Length);

        public override Fixed8 SystemFee => Fixed8.FromDecimal(500 * Contracts.Length);

        public PublishTransaction()
            : base(TransactionType.PublishTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Contracts = new byte[reader.ReadByte()][];
            if (Contracts.Length == 0) throw new FormatException();
            for (int i = 0; i < Contracts.Length; i++)
                Contracts[i] = reader.ReadVarBytes();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write((byte)Contracts.Length);
            for (int i = 0; i < Contracts.Length; i++)
                writer.WriteVarBytes(Contracts[i]);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["contracts"] = new JArray(Contracts.Select(p => new JString(p.ToHexString())));
            return json;
        }
    }
}
