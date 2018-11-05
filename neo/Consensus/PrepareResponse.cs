using System.IO;
using Neo.Network.P2P.Payloads;
using Neo.IO;

namespace Neo.Consensus
{
    internal class PrepareResponse : ConsensusMessage
    {
        public ConsensusPayload PreparePayload;
        public byte[] ResponseSignature;

        public PrepareResponse() : base(ConsensusMessageType.PrepareResponse) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            PreparePayload = new ConsensusPayload();
            ((ISerializable)PreparePayload).Deserialize(reader);
            ResponseSignature = reader.ReadBytes(64);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            ((ISerializable)PreparePayload).Serialize(writer);
            writer.Write(ResponseSignature);
        }
    }
}
