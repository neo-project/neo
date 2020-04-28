using Neo.IO;
using System.IO;

namespace Neo.SmartContract.NNS
{
    public class DomainInfo : ISerializable
    {
        public UInt160 Owner { set; get; }
        public UInt160 Operator { set; get; }
        public uint ValidUntilBlock { set; get; }
        public string Name { set; get; }

        public int Size => UInt160.Length + UInt160.Length + sizeof(ulong) + Name.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            Owner = reader.ReadSerializable<UInt160>();
            Operator = reader.ReadSerializable<UInt160>();
            ValidUntilBlock = reader.ReadUInt32();
            Name = reader.ReadVarString(1024);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Owner);
            writer.Write(Operator);
            writer.Write(ValidUntilBlock);
            writer.WriteVarString(Name);
        }
    }
}
