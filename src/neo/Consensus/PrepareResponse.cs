using Neo.IO;
using System.IO;

namespace Neo.Consensus
{
    public class PrepareResponse : ConsensusMessage
    {
        public UInt256 PreparationHash;

        public override int Size => base.Size + UInt256.Length;

        public PrepareResponse()
            : base(ConsensusMessageType.PrepareResponse)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            PreparationHash = reader.ReadSerializable<UInt256>();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(PreparationHash);
        }
    }
}
