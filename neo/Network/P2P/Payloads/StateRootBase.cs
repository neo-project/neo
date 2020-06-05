
using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class StateRootBase : ISerializable
    {
        public byte Version;
        public uint Index;
        public UInt256 PreHash;
        public UInt256 Root;

        public virtual int Size =>
             sizeof(byte) +          //Version
             sizeof(uint) +          //Index
             PreHash.Size +          //PrevHash
             Root.Size;             //StateRoot

        public void Deserialize(BinaryReader reader)
        {
            Version = reader.ReadByte();
            Index = reader.ReadUInt32();
            PreHash = reader.ReadSerializable<UInt256>();
            Root = reader.ReadSerializable<UInt256>();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Index);
            writer.Write(PreHash);
            writer.Write(Root);
        }
    }
}
