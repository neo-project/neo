using System.IO;
using Neo.IO;
using Neo.Ledger;

namespace Neo.Consensus
{
    internal class RegenerationMessage : PrepareRequest
    {
        public byte[][] WitnessInvocationScripts;
        public uint PrepareRequestPayloadTimestamp;

        private byte validatorCount;

        public RegenerationMessage(byte validatorCount) : base(ConsensusMessageType.RegenerationMessage)
        {
            this.validatorCount = validatorCount;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            reader.ReadVarInt(validatorCount);
            for (int i = 0; i < validatorCount; i++)
            {
                int signatureBytes = (int) reader.ReadVarInt(1024);
                if (signatureBytes == 0)
                    WitnessInvocationScripts[i] = null;
                else
                    WitnessInvocationScripts[i] = reader.ReadBytes(signatureBytes);
            }
            PrepareRequestPayloadTimestamp = reader.ReadUInt32();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarInt(validatorCount);
            for (int i = 0; i < validatorCount; i++)
            {
                if (WitnessInvocationScripts[i] == null)
                    writer.WriteVarInt(0);
                else
                    writer.WriteVarBytes(WitnessInvocationScripts[i]);
            }
            writer.Write(PrepareRequestPayloadTimestamp);
        }
    }
}