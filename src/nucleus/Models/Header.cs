using System;
using System.IO;
using System.Linq;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class Header : BlockBase, IEquatable<Header>
    {
        public Header(uint magic) : base(magic)
        {
        }

        public override int Size => base.Size + 1;

        public bool Equals(Header other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Header);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            if (reader.ReadByte() != 0) throw new FormatException();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write((byte)0);
        }
    }
}
