using System.IO;
using Neo.IO;
using Neo.Ledger;

namespace Neo.Consensus
{
    internal class RegenerationMessage : PrepareRequest
    {
        public byte[][] PrepareMsgWitnessInvocationScripts;
        public uint PrepareRequestPayloadTimestamp;
        public byte[][] ChangeViewWitnessInvocationScripts;

        public RegenerationMessage() : base(ConsensusMessageType.RegenerationMessage)
        {
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            PrepareMsgWitnessInvocationScripts = new byte[reader.ReadVarInt(255)][];
            for (int i = 0; i < PrepareMsgWitnessInvocationScripts.Length; i++)
            {
                int signatureBytes = (int) reader.ReadVarInt(1024);
                if (signatureBytes == 0)
                    PrepareMsgWitnessInvocationScripts[i] = null;
                else
                    PrepareMsgWitnessInvocationScripts[i] = reader.ReadBytes(signatureBytes);
            }
            PrepareRequestPayloadTimestamp = reader.ReadUInt32();
            ChangeViewWitnessInvocationScripts = new byte[reader.ReadVarInt(255)][];
            for (int i = 0; i < ChangeViewWitnessInvocationScripts.Length; i++)
            {
                int signatureBytes = (int) reader.ReadVarInt(1024);
                if (signatureBytes == 0)
                    ChangeViewWitnessInvocationScripts[i] = null;
                else
                    ChangeViewWitnessInvocationScripts[i] = reader.ReadBytes(signatureBytes);
            }
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarInt(PrepareMsgWitnessInvocationScripts.Length);
            foreach (var witnessInvocationScript in PrepareMsgWitnessInvocationScripts)
            {
                if (witnessInvocationScript == null)
                    writer.WriteVarInt(0);
                else
                    writer.WriteVarBytes(witnessInvocationScript);
            }
            writer.Write(PrepareRequestPayloadTimestamp);
            foreach (var witnessInvocationScript in PrepareMsgWitnessInvocationScripts)
            {
                if (witnessInvocationScript == null)
                    writer.WriteVarInt(0);
                else
                    writer.WriteVarBytes(witnessInvocationScript);
            }
        }
    }
}