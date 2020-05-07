using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Ledger
{
    public class HashIndexState : ICloneable<HashIndexState>, IO.ISerializable
    {
        public UInt256 Hash = UInt256.Zero;
        public uint Index = uint.MaxValue;

        int IO.ISerializable.Size => Hash.Size + sizeof(uint);

        HashIndexState ICloneable<HashIndexState>.Clone()
        {
            return new HashIndexState
            {
                Hash = Hash,
                Index = Index
            };
        }

        void IO.ISerializable.Deserialize(BinaryReader reader)
        {
            Hash = reader.ReadSerializable<UInt256>();
            Index = reader.ReadUInt32();
        }

        void ICloneable<HashIndexState>.FromReplica(HashIndexState replica)
        {
            Hash = replica.Hash;
            Index = replica.Index;
        }

        void IO.ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Hash);
            writer.Write(Index);
        }

        internal void Set(BlockBase block)
        {
            Hash = block.Hash;
            Index = block.Index;
        }
    }
}
