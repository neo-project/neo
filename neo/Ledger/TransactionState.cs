using Neo.IO;
using Neo.IO.Json;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Ledger
{
    public class TransactionState : StateBase, ICloneable<TransactionState>
    {
        public uint BlockIndex;
        public Transaction Transaction;

        public override int Size => base.Size + sizeof(uint) + Transaction.Size;

        TransactionState ICloneable<TransactionState>.Clone()
        {
            return new TransactionState
            {
                BlockIndex = BlockIndex,
                Transaction = Transaction
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            BlockIndex = reader.ReadUInt32();
            Transaction = Transaction.DeserializeFrom(reader);
        }

        void ICloneable<TransactionState>.FromReplica(TransactionState replica)
        {
            BlockIndex = replica.BlockIndex;
            Transaction = replica.Transaction;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(BlockIndex);
            writer.Write(Transaction);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["height"] = BlockIndex;
            json["tx"] = Transaction.ToJson();
            return json;
        }
    }
}
