using System.IO;

namespace Neo.Consensus
{
    internal class CommitAgreement : ConsensusMessage
    {
        /// <summary>
        /// Block signature
        /// </summary>
        public byte[] FinalSignature;

        /// <summary>
        /// Constructors
        /// </summary>
        public CommitAgreement() : base(ConsensusMessageType.CommitAgreement) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            FinalSignature = reader.ReadBytes(64);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(FinalSignature);
        }
    }
}