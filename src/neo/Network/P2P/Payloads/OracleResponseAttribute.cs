using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class OracleResponseAttribute : ISerializable
    {
        public UInt256 RequestTx;

        public int Size => UInt256.Length;

        public void Deserialize(BinaryReader reader)
        {
            RequestTx = reader.ReadSerializable<UInt256>();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(RequestTx);
        }

        public TransactionAttribute Build()
        {
            return new TransactionAttribute()
            {
                Usage = TransactionAttributeUsage.OracleResponse,
                Data = this.ToArray()
            };
        }
    }
}
