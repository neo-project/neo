using Neo.Cryptography;
using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class FilterLoadPayload : ISerializable
    {
        public byte[] Filter;
        public uint Tweak;

        public int Size => Filter.GetVarSize() + sizeof(uint);

        public static FilterLoadPayload Create(BloomFilter filter)
        {
            byte[] buffer = new byte[filter.M / 8];
            filter.GetBits(buffer);
            return new FilterLoadPayload
            {
                Filter = buffer,
                Tweak = filter.Tweak
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Filter = reader.ReadVarBytes(36000);
            Tweak = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Filter);
            writer.Write(Tweak);
        }
    }
}
