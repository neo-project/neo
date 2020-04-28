using Neo.IO;
using System.IO;

namespace Neo.SmartContract.NNS
{
    public class DomainInfo : ISerializable
    {
        public UInt160 Owner { set; get; }
        public UInt160 Manager { set; get; }
        public ulong TimeToLive { set; get; }
        public string Name { set; get; }

        public int Size => 20 + 20 + sizeof(ulong) + Name.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            Owner = reader.ReadSerializable<UInt160>();
            Manager = reader.ReadSerializable<UInt160>();
            TimeToLive = reader.ReadUInt64();
            Name = reader.ReadVarString(1024);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Owner);
            writer.Write(Manager);
            writer.Write(TimeToLive);
            writer.WriteVarString(Name);
        }
    }
}
