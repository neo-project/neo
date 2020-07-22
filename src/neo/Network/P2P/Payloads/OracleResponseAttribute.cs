using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class OracleResponseAttribute : TransactionAttribute
    {
        public UInt256 RequestTx;

        public override int Size => base.Size + UInt256.Length;
        public override bool AllowMultiple => false;

        public override TransactionAttributeType Type => TransactionAttributeType.OracleResponse;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            RequestTx = reader.ReadSerializable<UInt256>();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(RequestTx);
        }
    }
}
