using System.IO;

namespace AntShares.Consensus
{
    internal class PerpareResponse : ConsensusMessage
    {
        public byte[] Signature;

        public PerpareResponse()
            : base(ConsensusMessageType.PerpareResponse)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Signature = reader.ReadBytes(64);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Signature);
        }
    }
}
