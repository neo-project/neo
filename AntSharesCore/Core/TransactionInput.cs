using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class TransactionInput : ISerializable
    {
        public UInt256 PrevTxId;
        public UInt16 PrevIndex;

        public void Deserialize(BinaryReader reader)
        {
            this.PrevTxId = reader.ReadSerializable<UInt256>();
            this.PrevIndex = reader.ReadUInt16();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PrevTxId);
            writer.Write(PrevIndex);
        }
    }
}
