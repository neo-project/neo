using System.IO;
using Neo.Network.P2P.Payloads;
using Neo.IO;

namespace Neo.Consensus
{
    internal class PrepareResponse : ConsensusMessage
    {
        public ConsensusPayload PreparePayload;
        public byte[] ResponseSignature; // TODO: send multiple signatures for possible speedup?
        public PrepareRequest PrepareRequestMessage() {
            ConsensusMessage message;
            try
            {
                message = ConsensusMessage.DeserializeFrom(PreparePayload.Data);
            }
            catch
            {
                return new PrepareRequest();
            }
            return (PrepareRequest)message;
        }

        public PrepareResponse()
            : base(ConsensusMessageType.PrepareResponse)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            PreparePayload = new ConsensusPayload();
            ISerializable iss = PreparePayload;
            iss.Deserialize(reader);
            ResponseSignature = reader.ReadBytes(64);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            ISerializable iss = PreparePayload;
            iss.Serialize(writer);
            //writer.Write(PreparePayload);
            writer.Write(ResponseSignature);
        }
    }
}
