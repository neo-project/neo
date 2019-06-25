using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using System.IO;

namespace Neo.Ledger
{
    public class TransactionState : ICloneable<TransactionState>, ISerializable
    {
        public uint BlockIndex;
        public Transaction Transaction;
        public VMState Result;

        int ISerializable.Size => 
            sizeof(uint) +      // BlockIndex
            Transaction.Size +  // Transaction
            sizeof(byte);       // Result

        TransactionState ICloneable<TransactionState>.Clone()
        {
            return new TransactionState
            {
                BlockIndex = BlockIndex,
                Transaction = Transaction,
                Result = Result
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            BlockIndex = reader.ReadUInt32();
            Transaction = reader.ReadSerializable<Transaction>();
            Result = (VMState)reader.ReadByte();
        }

        void ICloneable<TransactionState>.FromReplica(TransactionState replica)
        {
            BlockIndex = replica.BlockIndex;
            Transaction = replica.Transaction;
            Result = replica.Result;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(BlockIndex);
            writer.Write(Transaction);
            writer.Write((byte)Result);
        }
    }
}
