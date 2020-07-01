using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class OracleResponseAttribute : TransactionAttribute, IInteroperable
    {
        public UInt256 RequestTxHash;
        public long FilterCost;
        public byte[] Data;

        public override int Size =>
            base.Size +                                 // Base size
            UInt256.Length +                            // Request tx hash
            sizeof(long) +                              // Filter cost
            (Data is null ? 1 : Data.GetVarSize());     // Data

        public override TransactionAttributeType Type => TransactionAttributeType.OracleResponse;

        public override bool AllowMultiple => false;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            RequestTxHash = new UInt256(reader.ReadBytes(UInt256.Length));
            Data = reader.ReadByte() == 0x01 ? reader.ReadVarBytes(ushort.MaxValue) : null;
            FilterCost = reader.ReadInt64();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(RequestTxHash);
            if (Data != null)
            {
                writer.Write((byte)0x01);
                writer.WriteVarBytes(Data);
            }
            else
            {
                writer.Write((byte)0x00);
            }
            writer.Write(FilterCost);
        }

        public void FromStackItem(StackItem stackItem) => throw new System.NotImplementedException();

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter)
            {
                RequestTxHash.ToArray(),
                Data,
                FilterCost
            };
        }
    }
}
