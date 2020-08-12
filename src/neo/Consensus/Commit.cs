using Neo.IO;
using System.IO;

namespace Neo.Consensus
{
    public class Commit : ConsensusMessage
    {
        public byte[] Signature;

        public override int Size => base.Size + 64;

        public Commit() : base(ConsensusMessageType.Commit) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Signature = reader.ReadFixedBytes(64);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Signature);
        }
    }
}
