using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class CosignerAttribute : TransactionAttribute
    {
        public override int Size => Cosigners.GetVarSize();
        public override TransactionAttributeUsage Usage => TransactionAttributeUsage.Cosigners;

        public Cosigner[] Cosigners { get; set; }

        public override void Deserialize(BinaryReader reader)
        {
            Cosigners = reader.ReadSerializableArray<Cosigner>(TransactionAttributeCollection.MaxTransactionAttributes);
            if (Cosigners.Select(u => u.Account).Distinct().Count() != Cosigners.Length) throw new FormatException();
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Cosigners);
        }

        protected override JObject ToJsonValue()
        {
            return new JArray(Cosigners.Select(u => u.ToJson()));
        }

        public static CosignerAttribute FromJsonValue(JObject json)
        {
            return new CosignerAttribute
            {
                Cosigners = ((JArray)json).Select(u => Cosigner.FromJson(json)).ToArray()
            };
        }
    }
}
