using AntShares.Algebra;
using AntShares.IO;
using System.IO;

namespace AntShares.Miner
{
    internal class BlockConsensusRequest : ISerializable
    {
        public UInt256 PrevHash;
        public FiniteFieldPoint[] NoncePieces;
        public UInt256 NonceHash;

        public void Deserialize(BinaryReader reader)
        {
            //TODO: 反序列化
        }

        public void Serialize(BinaryWriter writer)
        {
            //TODO: 序列化
        }
    }
}
