using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class OracleAttribute : ISerializable
    {
        public enum OracleAttributeType : byte
        {
            Request = 0,
            Response = 1
        }

        public UInt256 RequestTx;
        public OracleAttributeType Type;

        public int Size => sizeof(OracleAttributeType) + (Type == OracleAttributeType.Request ? 0 : UInt256.Length);

        public void Deserialize(BinaryReader reader)
        {
            Type = (OracleAttributeType)reader.ReadByte();
            if (!Enum.IsDefined(typeof(OracleAttributeType), Type)) throw new FormatException();

            if (Type == OracleAttributeType.Response)
            {
                RequestTx = reader.ReadSerializable<UInt256>();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            if (Type == OracleAttributeType.Response)
            {
                writer.Write(RequestTx);
            }
        }

        public TransactionAttribute Build()
        {
            return new TransactionAttribute()
            {
                Usage = TransactionAttributeUsage.Oracle,
                Data = this.ToArray()
            };
        }
    }
}
