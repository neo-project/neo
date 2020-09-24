using Neo.IO;
using Neo.Models;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using System.IO;

namespace Neo.Ledger
{
    public class TransactionState : ICloneable<TransactionState>, ISerializable
    {
        public uint BlockIndex;
        public VMState VMState;
        public Transaction Transaction;

        int ISerializable.Size =>
            sizeof(uint) +      // BlockIndex
            sizeof(VMState) +   // VMState
            Transaction.Size;   // Transaction

        TransactionState ICloneable<TransactionState>.Clone()
        {
            return new TransactionState
            {
                BlockIndex = BlockIndex,
                VMState = VMState,
                Transaction = Transaction
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            BlockIndex = reader.ReadUInt32();
            VMState = (VMState)reader.ReadByte();
            Transaction = reader.ReadSerializable(() => new Transaction(ProtocolSettings.Default.Magic));
        }

        void ICloneable<TransactionState>.FromReplica(TransactionState replica)
        {
            BlockIndex = replica.BlockIndex;
            VMState = replica.VMState;
            Transaction = replica.Transaction;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(BlockIndex);
            writer.Write((byte)VMState);
            writer.Write(Transaction);
        }
    }
}
