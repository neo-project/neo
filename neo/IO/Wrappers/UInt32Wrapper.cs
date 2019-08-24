using System;
using System.IO;

namespace Neo.IO.Wrappers
{
    public sealed class UInt32Wrapper : SerializableWrapper<uint>, IEquatable<UInt32Wrapper>
    {
        public override int Size => sizeof(uint);

        public UInt32Wrapper()
        {
        }

        private UInt32Wrapper(uint value)
        {
            this.value = value;
        }

        public override void Deserialize(BinaryReader reader)
        {
            value = reader.ReadUInt32();
        }

        public bool Equals(UInt32Wrapper other)
        {
            return value == other.value;
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public static implicit operator UInt32Wrapper(uint value)
        {
            return new UInt32Wrapper(value);
        }

        public static implicit operator uint(UInt32Wrapper wrapper)
        {
            return wrapper.value;
        }
    }
}
