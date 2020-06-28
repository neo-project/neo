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
            public ushort ValidatorIndex;
            public byte[] InvocationScript;
            public byte[] StateRootSignature;

            int ISerializable.Size =>
                sizeof(ushort) +                    //ValidatorIndex
                InvocationScript.GetVarSize() +     //InvocationScript
                StateRootSignature.Length;          //StateRootSignature

            void ISerializable.Deserialize(BinaryReader reader)
            {
                ValidatorIndex = reader.ReadUInt16();
                InvocationScript = reader.ReadVarBytes(1024);
                StateRootSignature = reader.ReadBytes(64);
            }

            public static PreparationPayloadCompact FromPayload(ConsensusPayload payload)
            {
                byte[] StateRootSignature = Array.Empty<byte>();
                ConsensusMessage message = payload.ConsensusMessage;
                if (message is PrepareRequest req)
                {
                    StateRootSignature = req.StateRootSignature;
                }
                else if (message is PrepareResponse resp)
                {
                    StateRootSignature = resp.StateRootSignature;
                }
                return new PreparationPayloadCompact
                {
                    ValidatorIndex = payload.ValidatorIndex,
                    InvocationScript = payload.Witness.InvocationScript,
                    StateRootSignature = StateRootSignature
                };
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ValidatorIndex);
                writer.WriteVarBytes(InvocationScript);
                writer.Write(StateRootSignature);
            }
        }
    }
}
