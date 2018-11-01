using System.IO;

namespace Neo.Consensus
{
    internal class PrepareResponse : ConsensusMessage
    {
        public byte[] Signature;

        public PrepareResponse()
            : base(ConsensusMessageType.PrepareResponse)
        {
        }

        public static PrepareResponse Make(byte[] signature)
        {
            return new PrepareResponse
            {
                Signature = signature
            };
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
