using System.IO;
using Neo.IO;

namespace Neo.Consensus
{
    internal class CommitAgreement : ConsensusMessage
    {
        /// <summary>
        /// Block hash of the signature
        /// </summary>
        public UInt256 BlockHash;

        /// <summary>
        /// Signature
        /// </summary>
        public byte[] Signature;

        /// <summary>
        /// Constructors
        /// </summary>
        public CommitAgreement() : base(ConsensusMessageType.CommitAgreement) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            BlockHash = reader.ReadSerializable<UInt256>();
            Signature = reader.ReadBytes(64);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            writer.Write(BlockHash);
            writer.Write(Signature);
        }
    }
}