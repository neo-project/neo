using AntShares.IO;
using AntShares.IO.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AntShares.Core
{
    public class InvocationTransaction : Transaction
    {
        public byte[] Script;
        public Fixed8 Gas;

        public override int Size => base.Size + Script.GetVarSize();

        public override Fixed8 NetworkFee => Gas;

        public InvocationTransaction()
            : base(TransactionType.InvocationTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Script = reader.ReadVarBytes(65536);
            if (Version >= 1)
            {
                Gas = reader.ReadSerializable<Fixed8>();
                if (Gas < Fixed8.Zero) throw new FormatException();
            }
            else
            {
                Gas = Fixed8.Zero;
            }
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            if (Version >= 1)
                writer.Write(Gas);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["script"] = Script.ToHexString();
            json["gas"] = Gas.ToString();
            return json;
        }

        public override bool Verify(IEnumerable<Transaction> mempool)
        {
            // Not available in MAINNET until tested
            if (Settings.Default.Magic != 1953787457) return false;
            if (Version == 0) return false;
            return base.Verify(mempool);
        }
    }
}
