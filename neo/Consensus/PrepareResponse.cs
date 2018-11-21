using System.IO;
using Neo.Network.P2P.Payloads;
using Neo.IO;

namespace Neo.Consensus
{
    internal class PrepareResponse : ConsensusMessage
    {
        /// <summary>
        /// Speaker PrepareRequest Payload
        /// </summary>
        public ConsensusPayload PreparePayload;
        /// <summary>
        /// Prepare Request, PreparePayload, payload signature
        /// </summary>
        public byte[] ResponseSignature;

        public override int Size => base.Size + PreparePayload.Size + ResponseSignature.Length;

        public PrepareResponse()
            : base(ConsensusMessageType.PrepareResponse)
        {
        }

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
