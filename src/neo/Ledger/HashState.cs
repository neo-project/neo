using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Ledger
{
    public class HashState : ICloneable<HashState>, ISerializable
    {
        public UInt256 Hash = UInt256.Zero;

        int ISerializable.Size => Hash.Size;

        HashState ICloneable<HashState>.Clone()
        {
            return new HashState
            {
                Hash = Hash
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Hash = reader.ReadSerializable<UInt256>();
        }

        void ICloneable<HashState>.FromReplica(HashState replica)
        {
            Hash = replica.Hash;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Hash);
        }

        internal void Set(BlockBase block)
        {
            Hash = block.Hash;
        }
    }
}
