using System.IO;
using Neo.IO;
using Neo.VM;

namespace Neo.Network.P2P.Payloads
{
    public class ExecutedTransaction : ISerializable
    {
        public Transaction Transaction;
        public VMState State;

        public int Size => sizeof(VMState) + Transaction.Size;

        public void Deserialize(BinaryReader reader)
        {
            Transaction = Transaction.DeserializeFrom(reader);
            State = (VMState)reader.ReadByte();
        }

        public void Serialize(BinaryWriter writer)
        {
            ((ISerializable)Transaction).Serialize(writer);
            writer.Write((byte)State);
        }
    }
}