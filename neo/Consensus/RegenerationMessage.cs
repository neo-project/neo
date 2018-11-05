using System.IO;
using Neo.Network.P2P.Payloads;
using Neo.IO;

namespace Neo.Consensus
{
    internal class Renegeration : ConsensusMessage
    {
        /// <summary>
        /// Block signature
        /// </summary>
        public ConsensusPayload PrepareRequestPayload; 
        public byte[][] SignedPayloads;

        /// <summary>
        /// Constructors
        /// </summary>
        public Renegeration() : base(ConsensusMessageType.Renegeration) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            PrepareRequestPayload = new ConsensusPayload();
            ((ISerializable)PrepareRequestPayload).Deserialize(reader);
            int nValidators = reader.ReadInt32();
            for (int sp = 0; sp < nValidators; sp++)
                SignedPayloads[sp] = reader.ReadBytes(64);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            ((ISerializable)PrepareRequestPayload).Serialize(writer);
            writer.Write(PrepareRequestPayload);
            writer.Write(SignedPayloads.Length);

            for (int sp = 0; sp < SignedPayloads.Length;sp++)
                writer.Write(SignedPayloads[sp]);
        }
    }
}