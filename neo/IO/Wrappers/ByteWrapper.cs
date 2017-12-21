using System.IO;

namespace Neo.IO.Wrappers
{
    internal class ByteWrapper : SerializableWrapper<byte>
    {
        private byte value;

        public override int Size => sizeof(byte);

        public ByteWrapper(byte value)
        {
            this.value = value;
        }

        public override void Deserialize(BinaryReader reader)
        {
            value = reader.ReadByte();
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(value);
        }
    }
}
