using System.IO;
using Neo.IO;

namespace Neo.Models
{
    public class ConsensusData : ISerializable
    {
        public byte PrimaryIndex;
        public ulong Nonce;

        public int Size => sizeof(byte) + sizeof(ulong);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            PrimaryIndex = reader.ReadByte();
            Nonce = reader.ReadUInt64();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(PrimaryIndex);
            writer.Write(Nonce);
        }
    }
}
