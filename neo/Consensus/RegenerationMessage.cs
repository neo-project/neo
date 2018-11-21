using System.IO;
using System.Linq;
using Neo.Network.P2P.Payloads;
using Neo.IO;

namespace Neo.Consensus
{
    internal class Regeneration : ConsensusMessage
    {
        /// <summary>
        /// Speaker PrepareRequest in which, at least, M nodes signed
        /// </summary>
        public ConsensusPayload PrepareRequestPayload; 
        /// <summary>
        /// Partial signatures of, at least, M nodes
        /// </summary>
        public byte[][] SignedPayloads;

        public override int Size => base.Size + PrepareRequestPayload.Size + sizeof(int) + SignedPayloads.Sum(u => u.Length);

        /// <summary>
        /// Constructors
        /// </summary>
        public Regeneration() : base(ConsensusMessageType.Regeneration) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            PrepareRequestPayload = new ConsensusPayload();
            ((ISerializable)PrepareRequestPayload).Deserialize(reader);

            int nValidators = reader.ReadInt32();
            SignedPayloads = new byte[nValidators][];

            for (int sp = 0; sp < nValidators; sp++)
            {
                SignedPayloads[sp] = reader.ReadBytes(64);
                if (SignedPayloads[sp].All(b => b == 0))
                    SignedPayloads[sp] = null;
            }
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            ((ISerializable)PrepareRequestPayload).Serialize(writer);

            writer.Write(SignedPayloads.Length);

            for (int sp = 0; sp < SignedPayloads.Length; sp++)
            {
                if (SignedPayloads[sp] != null)
                {
                    writer.Write(SignedPayloads[sp]);
                }
                else
                {
                    writer.Write(new byte[64]);
                }
            }
        }
    }
}
