using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.Payloads
{
    public class FilterLoadPayload : ISerializable
    {
        public byte[] Filter;
        public byte K;
        public uint Tweak;

        public int Size => Filter.GetVarSize() + sizeof(byte) + sizeof(uint);

        public static FilterLoadPayload Create(BloomFilter filter)
        {
            byte[] buffer = new byte[filter.M / 8];
            filter.GetBits(buffer);
            return new FilterLoadPayload
            {
                Filter = buffer,
                K = (byte)filter.K,
                Tweak = filter.Tweak
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Filter = reader.ReadVarBytes(36000);
            K = reader.ReadByte();
            if (K > 50) throw new FormatException();
            Tweak = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Filter);
            writer.Write(K);
            writer.Write(Tweak);
        }
    }
}
