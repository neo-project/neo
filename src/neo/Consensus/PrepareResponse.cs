using Neo.IO;
using System.IO;

namespace Neo.Consensus
{
    public class PrepareResponse : ConsensusMessage
    {
        public UInt256 PreparationHash;
        public byte[] StateRootSignature;
        public override int Size => base.Size + PreparationHash.Size + StateRootSignature.GetVarSize();

        public PrepareResponse()
            : base(ConsensusMessageType.PrepareResponse)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            PreparationHash = reader.ReadSerializable<UInt256>();
            StateRootSignature = reader.ReadVarBytes(1024);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(PreparationHash);
            writer.WriteVarBytes(StateRootSignature);
        }
    }
}
