using System.IO;
using Neo.Network.P2P.Payloads;

namespace Neo.Consensus
{
    internal class PrepareResponse : ConsensusMessage
    {
        public ConsensusPayload PreparePayload;
        public byte[] ResponseSignature; // TODO: send multiple signatures for possible speedup?

        public PrepareResponse()
            : base(ConsensusMessageType.PrepareResponse)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            PreparePayload = reader.ReadSerializable<ConsensusPayload>();
            ResponseSignature = reader.ReadBytes(16);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(PreparePayload);
            writer.Write(ResponseSignature);
        }
    }
}
