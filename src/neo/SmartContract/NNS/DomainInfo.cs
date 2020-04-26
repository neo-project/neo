using Neo.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.SmartContract.NNS
{
    public class DomainInfo : ISerializable
    {
        public UInt160 Owner { set; get; }
        public UInt160 Admin { set; get; }
        public ulong TimeToLive { set; get; }
        public string Name { set; get; }

        public int Size => 20 + 20 + sizeof(ulong) + Name.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            Owner = reader.ReadSerializable<UInt160>();
            Admin = reader.ReadSerializable<UInt160>();
            TimeToLive = reader.ReadUInt64();
            Name = reader.ReadVarString(1024);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Owner);
            writer.Write(Admin);
            writer.Write(TimeToLive);
            writer.WriteVarString(Name);
        }
    }

    public class RecordInfo : ISerializable
    {
        public RecordType RecordType { set; get; }
        public string Text { get; set; }

        public int Size => 1 + Text.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            RecordType = (RecordType)reader.ReadByte();
            Text = reader.ReadVarString(1024);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)RecordType);
            writer.WriteVarString(Text);
        }

        public override string ToString()
        {
            return "RecordType is: " + RecordType + " and Text is: " + Text;
        }
    }

    public enum RecordType : byte
    {
        A = 0x00,
        CNAME = 0x01,
        TXT = 0x10,
        NS = 0x11
    }
}
