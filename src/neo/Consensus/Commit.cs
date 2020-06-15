using Neo.IO;
using System.IO;

namespace Neo.Consensus
{
    public class Commit : ConsensusMessage
    {
        public byte[] BlockSignature;
        public byte[] StateRootSignature;
        public override int Size => base.Size + BlockSignature.Length + StateRootSignature.Length;

        public Commit() : base(ConsensusMessageType.Commit) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            BlockSignature = reader.ReadFixedBytes(64);
            StateRootSignature = reader.ReadFixedBytes(64);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(BlockSignature);
            writer.Write(StateRootSignature);
        }
    }
}
