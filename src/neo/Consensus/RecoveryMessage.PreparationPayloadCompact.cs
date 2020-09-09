using Neo.IO;
using Neo.Network.P2P.Payloads;
using System;
using System.IO;

namespace Neo.Consensus
{
    partial class RecoveryMessage
    {
        public class PreparationPayloadCompact : ISerializable
        {
            public byte ValidatorIndex;
            public byte[] InvocationScript;

            int ISerializable.Size =>
                sizeof(byte) +                  //ValidatorIndex
                InvocationScript.GetVarSize();  //InvocationScript

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ValidatorIndex = reader.ReadByte();
                if (ValidatorIndex >= ProtocolSettings.Default.ValidatorsCount)
                    throw new FormatException();
                InvocationScript = reader.ReadVarBytes(1024);
            }

            public static PreparationPayloadCompact FromPayload(ConsensusPayload payload)
            {
                return new PreparationPayloadCompact
                {
                    ValidatorIndex = payload.ValidatorIndex,
                    InvocationScript = payload.Witness.InvocationScript
                };
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ValidatorIndex);
                writer.WriteVarBytes(InvocationScript);
            }
        }
    }
}
