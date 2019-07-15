using System.IO;

namespace Neo.Consensus
{
    public class RecoveryRequest : ConsensusMessage
    {
        /// <summary>
        /// Timestamp of when the ChangeView message was created. This allows receiving nodes to ensure
        /// they only respond once to a specific RecoveryRequest request.
        /// In this sense, it prevents replay of the RecoveryRequest message from the repeatedly broadcast of Recovery's messages.
        /// </summary>
        public long Timestamp;

        public override int Size => base.Size
            + sizeof(long); //Timestamp

        public RecoveryRequest() : base(ConsensusMessageType.RecoveryRequest) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Timestamp = reader.ReadInt64();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Timestamp);
        }
    }
}
