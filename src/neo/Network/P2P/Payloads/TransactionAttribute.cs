using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class TransactionAttribute : ISerializable
    {
        public TransactionAttributeUsage Usage;
        public byte[] Data;

        public int Size => sizeof(TransactionAttributeUsage) + Data.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Usage = (TransactionAttributeUsage)reader.ReadByte();
            if (!Enum.IsDefined(typeof(TransactionAttributeUsage), Usage))
                throw new FormatException();
            Data = reader.ReadVarBytes(252);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Usage);
            writer.WriteVarBytes(Data);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["usage"] = Usage;
            json["data"] = Convert.ToBase64String(Data);
            return json;
        }

        public static TransactionAttribute FromJson(JObject json)
        {
            TransactionAttribute transactionAttribute = new TransactionAttribute();
            transactionAttribute.Usage = (TransactionAttributeUsage)byte.Parse(json["usage"].AsString());
            if (!Enum.IsDefined(typeof(TransactionAttributeUsage), transactionAttribute.Usage))
                throw new ArgumentException();
            transactionAttribute.Data = Convert.FromBase64String(json["data"].AsString());
            return transactionAttribute;
        }
    }
}
