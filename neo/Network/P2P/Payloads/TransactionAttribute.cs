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

        public int Size
        {
            get
            {
                if (Usage == TransactionAttributeUsage.Script)
                    return sizeof(TransactionAttributeUsage) + 20;
                else
                    return sizeof(TransactionAttributeUsage) + Data.GetVarSize();
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Usage = (TransactionAttributeUsage)reader.ReadByte();
            if (Usage == TransactionAttributeUsage.Script)
                Data = reader.ReadBytes(20);
            else if (Usage == TransactionAttributeUsage.Url)
                Data = reader.ReadVarBytes(252);
            else
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Usage);
            if (Usage == TransactionAttributeUsage.Script)
                writer.Write(Data);
            else
                writer.WriteVarBytes(Data);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["usage"] = Usage;
            json["data"] = Data.ToHexString();
            return json;
        }
    }
}
