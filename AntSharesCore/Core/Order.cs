using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class Order : ISerializable
    {
        public const byte OrderType = 0;
        public UInt256 AssetType;
        public UInt256 ValueType;
        public Int64 Amount;
        public UInt64 Price;
        public UInt160 ScriptHash;
        public UInt160 Agent;
        public TransactionInput[] Inputs;
        public byte[][] Scripts;

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != OrderType)
                throw new FormatException();
            this.AssetType = reader.ReadSerializable<UInt256>();
            this.ValueType = reader.ReadSerializable<UInt256>();
            this.Amount = reader.ReadInt64();
            this.Price = reader.ReadUInt64();
            this.ScriptHash = reader.ReadSerializable<UInt160>();
            this.Agent = reader.ReadSerializable<UInt160>();
            this.Inputs = reader.ReadSerializableArray<TransactionInput>();
            this.Scripts = reader.ReadBytesArray();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(OrderType);
            writer.Write(AssetType);
            writer.Write(ValueType);
            writer.Write(Amount);
            writer.Write(Price);
            writer.Write(ScriptHash);
            writer.Write(Agent);
            writer.Write(Inputs);
            writer.Write(Scripts);
        }
    }
}
