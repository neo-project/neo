using Neo.IO;
using Neo.VM;
using System;
using System.IO;

namespace Neo.Core
{
    public class Header : BlockBase, IEquatable<Header>
    {
        public override int Size => base.Size + 1;

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            if (reader.ReadByte() != 0) throw new FormatException();
        }

        public bool Equals(Header other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Header);
        }

        public static Header FromTrimmedData(byte[] data, int index)
        {
            Header header = new Header();
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                ((IVerifiable)header).DeserializeUnsigned(reader);
                reader.ReadByte(); header.Script = reader.ReadSerializable<Witness>();
            }
            return header;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write((byte)0);
        }
    }
}
